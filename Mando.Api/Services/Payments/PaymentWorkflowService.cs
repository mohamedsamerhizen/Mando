using Microsoft.EntityFrameworkCore;
using Mando.Api.Common;
using Mando.Api.Data;
using Mando.Api.DTOs.Payments;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Interfaces.Payments;
using Mando.Api.Interfaces.Users;
using Mando.Api.Interfaces.Visits;
using Mando.Api.Models.Financials;
using Mando.Api.Models.Payments;

namespace Mando.Api.Services.Payments;

public class PaymentWorkflowService : IPaymentWorkflowService
{
    private const int DocumentNumberCollisionRetryLimit = 3;
    private const int ApprovalStaleThresholdHours = 24;
    private const decimal HighBalanceImpactRatioThreshold = 0.80m;

    private readonly AppDbContext _context;
    private readonly IWorkflowSideEffectService _workflowSideEffectService;
    private readonly ICustomerBalanceService _customerBalanceService;
    private readonly ICustomerFinancialLockService _customerFinancialLockService;
    private readonly IDocumentNumberGenerator _documentNumberGenerator;
    private readonly IUserStatusLockService _userStatusLockService;
    private readonly IVisitLifecycleLockService _visitLifecycleLockService;

    public PaymentWorkflowService(
        AppDbContext context,
        IWorkflowSideEffectService workflowSideEffectService,
        ICustomerBalanceService customerBalanceService,
        ICustomerFinancialLockService customerFinancialLockService,
        IDocumentNumberGenerator documentNumberGenerator,
        IUserStatusLockService userStatusLockService,
        IVisitLifecycleLockService visitLifecycleLockService)
    {
        _context = context;
        _workflowSideEffectService = workflowSideEffectService;
        _customerBalanceService = customerBalanceService;
        _customerFinancialLockService = customerFinancialLockService;
        _documentNumberGenerator = documentNumberGenerator;
        _userStatusLockService = userStatusLockService;
        _visitLifecycleLockService = visitLifecycleLockService;
    }

    public async Task<PaymentWorkflowResult> CreateAsync(CreatePaymentRequestDto request, AppUser currentUser)
    {
        if (request.Amount <= 0)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.InvalidAmount };

        if (!Enum.IsDefined(request.PaymentMethod))
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.InvalidPaymentMethod };

        var normalizedReference = PaymentReferenceNormalizer.Normalize(request.Reference);
        if (request.PaymentMethod != PaymentMethod.Cash && normalizedReference is null)
        {
            return new PaymentWorkflowResult
            {
                Status = PaymentWorkflowStatus.NonCashReferenceRequired
            };
        }

        Payment? createdPaymentEntity = null;
        decimal currentBalance = 0m;

        for (var attempt = 0; attempt < DocumentNumberCollisionRetryLimit; attempt++)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userLockAcquired = await _userStatusLockService.LockAsync(currentUser.Id);
                if (!userLockAcquired)
                {
                    await transaction.RollbackAsync();
                    return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.Forbidden };
                }

                var lockedSalesRep = await _context.Users.FirstOrDefaultAsync(x => x.Id == currentUser.Id);
                if (lockedSalesRep is null || !lockedSalesRep.IsActive)
                {
                    await transaction.RollbackAsync();
                    return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.Forbidden };
                }

                var visitLockAcquired = await _visitLifecycleLockService.LockAsync(request.VisitId);
                if (!visitLockAcquired)
                {
                    await transaction.RollbackAsync();
                    return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.VisitNotFound };
                }

                var visit = await _context.Visits
                    .Include(x => x.Customer)
                    .FirstOrDefaultAsync(x => x.Id == request.VisitId);

                if (visit is null)
                {
                    await transaction.RollbackAsync();
                    return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.VisitNotFound };
                }

                if (visit.SalesRepId != currentUser.Id)
                {
                    await transaction.RollbackAsync();
                    return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.Forbidden };
                }

                var customerLockAcquired = await _customerFinancialLockService.LockAsync(visit.CustomerId);
                if (!customerLockAcquired)
                {
                    await transaction.RollbackAsync();

                    return new PaymentWorkflowResult
                    {
                        Status = PaymentWorkflowStatus.CustomerNotFound
                    };
                }

                visit = await _context.Visits
                    .Include(x => x.Customer)
                    .FirstOrDefaultAsync(x => x.Id == request.VisitId);

                if (visit is null)
                {
                    await transaction.RollbackAsync();
                    return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.VisitNotFound };
                }

                if (visit.Status != VisitStatus.InProgress)
                {
                    await transaction.RollbackAsync();
                    return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.VisitNotInProgress };
                }

                if (visit.Customer.Status != CustomerStatus.Active)
                {
                    await transaction.RollbackAsync();
                    return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.CustomerInactive };
                }

                var balanceSnapshot = await _customerBalanceService.GetSnapshotAsync(visit.CustomerId);
                if (balanceSnapshot is null)
                {
                    await transaction.RollbackAsync();

                    return new PaymentWorkflowResult
                    {
                        Status = PaymentWorkflowStatus.CustomerNotFound
                    };
                }

                currentBalance = balanceSnapshot.CurrentBalance;

                if (currentBalance <= 0)
                {
                    await transaction.RollbackAsync();

                    return new PaymentWorkflowResult
                    {
                        Status = PaymentWorkflowStatus.NoOutstandingBalance,
                        CurrentBalance = currentBalance
                    };
                }

                if (request.Amount > currentBalance)
                {
                    await transaction.RollbackAsync();

                    return new PaymentWorkflowResult
                    {
                        Status = PaymentWorkflowStatus.PaymentAmountExceedsBalance,
                        CurrentBalance = currentBalance
                    };
                }

                var pendingSubmittedAmount = await _context.Payments
                    .AsNoTracking()
                    .Where(x =>
                        x.CustomerId == visit.CustomerId &&
                        x.Status == PaymentStatus.Pending)
                    .SumAsync(x => (decimal?)x.Amount) ?? 0m;

                if (pendingSubmittedAmount + request.Amount > currentBalance)
                {
                    await transaction.RollbackAsync();

                    return new PaymentWorkflowResult
                    {
                        Status = PaymentWorkflowStatus.PendingPaymentsWouldExceedBalance,
                        CurrentBalance = currentBalance
                    };
                }

                if (normalizedReference is not null)
                {
                    var pendingReferences = await _context.Payments
                        .AsNoTracking()
                        .Where(x =>
                            x.CustomerId == visit.CustomerId &&
                            x.Status == PaymentStatus.Pending &&
                            x.Reference != null)
                        .Select(x => x.Reference)
                        .ToListAsync();

                    var duplicatePendingReferenceExists = pendingReferences
                        .Any(reference => PaymentReferenceNormalizer.AreEquivalent(reference, normalizedReference));

                    if (duplicatePendingReferenceExists)
                    {
                        await transaction.RollbackAsync();

                        return new PaymentWorkflowResult
                        {
                            Status = PaymentWorkflowStatus.DuplicatePendingReference,
                            CurrentBalance = currentBalance
                        };
                    }
                }

                var now = DateTime.UtcNow;
                var recentPotentialDuplicates = await _context.Payments
                    .AsNoTracking()
                    .Where(x =>
                        x.VisitId == visit.Id &&
                        x.SalesRepId == currentUser.Id &&
                        x.Status == PaymentStatus.Pending &&
                        x.Amount == request.Amount &&
                        x.PaymentMethod == request.PaymentMethod &&
                        x.CreatedAtUtc >= now.AddMinutes(-2))
                    .Select(x => x.Reference)
                    .ToListAsync();

                if (recentPotentialDuplicates.Any(existingReference => IsEquivalentOrBothMissing(existingReference, normalizedReference)))
                {
                    await transaction.RollbackAsync();

                    return new PaymentWorkflowResult
                    {
                        Status = PaymentWorkflowStatus.DuplicateSubmission,
                        CurrentBalance = currentBalance
                    };
                }

                var paymentNumber = await _documentNumberGenerator.GeneratePaymentNumberAsync();

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    PaymentNumber = paymentNumber,
                    VisitId = visit.Id,
                    CustomerId = visit.CustomerId,
                    SalesRepId = currentUser.Id,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    Status = PaymentStatus.Pending,
                    Reference = normalizedReference,
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    CreatedAtUtc = now
                };

                _context.Payments.Add(payment);

                _context.PaymentActionHistories.Add(CreateHistoryEntry(
                    paymentId: payment.Id,
                    actionType: PaymentActionType.Submitted,
                    previousStatus: null,
                    newStatus: PaymentStatus.Pending,
                    performedByUser: currentUser,
                    balanceBeforeAction: currentBalance,
                    balanceAfterAction: currentBalance,
                    comment: "Payment submitted and is awaiting review."));

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                await _workflowSideEffectService.CreateNotificationForRolesAsync(
                    [AppRoles.Admin, AppRoles.Manager],
                    NotificationType.PaymentSubmitted,
                    "New payment submitted",
                    $"A payment '{payment.PaymentNumber}' was submitted by '{currentUser.FullName}' for customer '{visit.Customer.Name}'.",
                    payment.Id);

                await _workflowSideEffectService.WriteAuditAsync(
                    currentUser.Id,
                    AuditActionType.PaymentCreated,
                    nameof(Payment),
                    payment.Id,
                    $"Payment '{payment.PaymentNumber}' was created for customer '{visit.Customer.Name}' by sales rep '{currentUser.FullName}' with amount {payment.Amount:0.00}. Outstanding balance at submission: {currentBalance:0.00}.");
                createdPaymentEntity = payment;
                break;
            }
            catch (DbUpdateException ex) when (DbUpdateExceptionHelper.IsUniqueConstraintViolation(ex))
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                if (attempt == DocumentNumberCollisionRetryLimit - 1)
                    throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        if (createdPaymentEntity is null)
            throw new InvalidOperationException("Failed to create payment after retrying document number collisions.");

        var createdPayment = await LoadPaymentAsync(createdPaymentEntity.Id);

        return new PaymentWorkflowResult
        {
            Status = PaymentWorkflowStatus.Success,
            Payment = createdPayment,
            CurrentBalance = currentBalance
        };
    }

    public async Task<PaymentWorkflowResult> ApproveAsync(Guid paymentId, ApprovePaymentRequestDto request, AppUser currentUser)
    {
        var normalizedReviewComment = request.ReviewComment?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReviewComment))
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.ApprovalReviewCommentRequired };

        var payment = await LoadPaymentAsync(paymentId);
        if (payment is null)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentNotFound };

        if (payment.SalesRepId == currentUser.Id)
        {
            return new PaymentWorkflowResult
            {
                Status = PaymentWorkflowStatus.PrivilegedSelfReviewForbidden,
                Payment = payment
            };
        }

        if (!RowVersionTokenHelper.TryDecode(request.RowVersion, out var originalRowVersion))
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.InvalidConcurrencyToken };

        if (payment.Status == PaymentStatus.Approved)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentAlreadyApproved, Payment = payment };

        if (payment.Status == PaymentStatus.Rejected)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentAlreadyRejected, Payment = payment };

        if (payment.Status != PaymentStatus.Pending)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentNotPending, Payment = payment };

        _context.Entry(payment).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

        CustomerBalanceSnapshot? balanceSnapshot = null;
        ApprovalDecisionSignals? approvalSignals = null;
        decimal newBalanceAfterApproval = 0m;
        string approvalHistoryComment = string.Empty;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var customerLockAcquired = await _customerFinancialLockService.LockAsync(payment.CustomerId);
            if (!customerLockAcquired)
            {
                await transaction.RollbackAsync();

                return new PaymentWorkflowResult
                {
                    Status = PaymentWorkflowStatus.CustomerNotFound,
                    Payment = payment
                };
            }

            balanceSnapshot = await _customerBalanceService.GetSnapshotAsync(payment.CustomerId);
            if (balanceSnapshot is null)
            {
                await transaction.RollbackAsync();

                return new PaymentWorkflowResult
                {
                    Status = PaymentWorkflowStatus.CustomerNotFound,
                    Payment = payment
                };
            }

            if (balanceSnapshot.CurrentBalance <= 0)
            {
                await transaction.RollbackAsync();

                return new PaymentWorkflowResult
                {
                    Status = PaymentWorkflowStatus.NoOutstandingBalance,
                    Payment = payment,
                    CurrentBalance = balanceSnapshot.CurrentBalance
                };
            }

            if (payment.Amount > balanceSnapshot.CurrentBalance)
            {
                await transaction.RollbackAsync();

                return new PaymentWorkflowResult
                {
                    Status = PaymentWorkflowStatus.PaymentAmountExceedsBalance,
                    Payment = payment,
                    CurrentBalance = balanceSnapshot.CurrentBalance
                };
            }

            approvalSignals = await BuildApprovalDecisionSignalsAsync(payment, balanceSnapshot.CurrentBalance);

            if (approvalSignals.MissingReferenceForNonCash)
            {
                await transaction.RollbackAsync();

                return new PaymentWorkflowResult
                {
                    Status = PaymentWorkflowStatus.NonCashReferenceRequiredForApproval,
                    Payment = payment,
                    CurrentBalance = balanceSnapshot.CurrentBalance
                };
            }

            if (approvalSignals.IsStale && !request.AcknowledgeStalePayment)
            {
                await transaction.RollbackAsync();

                return new PaymentWorkflowResult
                {
                    Status = PaymentWorkflowStatus.ApprovalStaleAcknowledgementRequired,
                    Payment = payment,
                    CurrentBalance = balanceSnapshot.CurrentBalance
                };
            }

            if (approvalSignals.HasHighBalanceImpact && !request.AcknowledgeHighBalanceImpact)
            {
                await transaction.RollbackAsync();

                return new PaymentWorkflowResult
                {
                    Status = PaymentWorkflowStatus.ApprovalHighBalanceImpactAcknowledgementRequired,
                    Payment = payment,
                    CurrentBalance = balanceSnapshot.CurrentBalance
                };
            }

            if (approvalSignals.HasMultiplePendingPayments && !request.AcknowledgeMultiplePendingPayments)
            {
                await transaction.RollbackAsync();

                return new PaymentWorkflowResult
                {
                    Status = PaymentWorkflowStatus.ApprovalMultiplePendingAcknowledgementRequired,
                    Payment = payment,
                    CurrentBalance = balanceSnapshot.CurrentBalance
                };
            }

            if (approvalSignals.HasDuplicatePendingReference && !request.AcknowledgeDuplicateReference)
            {
                await transaction.RollbackAsync();

                return new PaymentWorkflowResult
                {
                    Status = PaymentWorkflowStatus.ApprovalDuplicateReferenceAcknowledgementRequired,
                    Payment = payment,
                    CurrentBalance = balanceSnapshot.CurrentBalance
                };
            }

            var previousStatus = payment.Status;
            var decisionTimeUtc = DateTime.UtcNow;
            newBalanceAfterApproval = balanceSnapshot.CurrentBalance - payment.Amount;
            approvalHistoryComment = BuildApprovalHistoryComment(normalizedReviewComment, approvalSignals);

            payment.Status = PaymentStatus.Approved;
            payment.ReviewedByUserId = currentUser.Id;
            payment.ReviewedAtUtc = decisionTimeUtc;
            payment.RejectionReason = null;
            payment.UpdatedAtUtc = decisionTimeUtc;

            _context.PaymentActionHistories.Add(CreateHistoryEntry(
                paymentId: payment.Id,
                actionType: PaymentActionType.Approved,
                previousStatus: previousStatus,
                newStatus: PaymentStatus.Approved,
                performedByUser: currentUser,
                balanceBeforeAction: balanceSnapshot.CurrentBalance,
                balanceAfterAction: newBalanceAfterApproval,
                comment: approvalHistoryComment));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.CreateNotificationForUserAsync(
                payment.SalesRepId,
                NotificationType.PaymentApproved,
                "Payment approved",
                $"Your payment '{payment.PaymentNumber}' for customer '{payment.Customer.Name}' was approved.",
                payment.Id);

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.PaymentApproved,
                nameof(Payment),
                payment.Id,
                $"Payment '{payment.PaymentNumber}' for customer '{payment.Customer.Name}' was approved by '{currentUser.FullName}' with amount {payment.Amount:0.00}. Balance before approval: {balanceSnapshot.CurrentBalance:0.00}. Balance after approval: {newBalanceAfterApproval:0.00}. {approvalHistoryComment}");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            return new PaymentWorkflowResult
            {
                Status = PaymentWorkflowStatus.ConcurrencyConflict
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var updatedPayment = await LoadPaymentAsync(paymentId);

        return new PaymentWorkflowResult
        {
            Status = PaymentWorkflowStatus.Success,
            Payment = updatedPayment,
            CurrentBalance = newBalanceAfterApproval
        };
    }

    public async Task<PaymentWorkflowResult> RejectAsync(Guid paymentId, RejectPaymentRequestDto request, AppUser currentUser)
    {
        var normalizedReason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.RejectionReasonRequired };

        if (!request.Category.HasValue)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.RejectionCategoryRequired };

        var payment = await LoadPaymentAsync(paymentId);
        if (payment is null)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentNotFound };

        if (payment.SalesRepId == currentUser.Id)
        {
            return new PaymentWorkflowResult
            {
                Status = PaymentWorkflowStatus.PrivilegedSelfReviewForbidden,
                Payment = payment
            };
        }

        if (!RowVersionTokenHelper.TryDecode(request.RowVersion, out var originalRowVersion))
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.InvalidConcurrencyToken };

        if (payment.Status == PaymentStatus.Rejected)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentAlreadyRejected, Payment = payment };

        if (payment.Status == PaymentStatus.Approved)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentAlreadyApproved, Payment = payment };

        if (payment.Status != PaymentStatus.Pending)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentNotPending, Payment = payment };

        _context.Entry(payment).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

        var balanceSnapshot = await _customerBalanceService.GetSnapshotAsync(payment.CustomerId);
        var balanceAtRejection = balanceSnapshot?.CurrentBalance;

        var previousStatus = payment.Status;
        var decisionTimeUtc = DateTime.UtcNow;
        var rejectionComment = BuildRejectionComment(request.Category.Value, normalizedReason);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            payment.Status = PaymentStatus.Rejected;
            payment.ReviewedByUserId = currentUser.Id;
            payment.ReviewedAtUtc = decisionTimeUtc;
            payment.RejectionReason = rejectionComment;
            payment.UpdatedAtUtc = decisionTimeUtc;

            _context.PaymentActionHistories.Add(CreateHistoryEntry(
                paymentId: payment.Id,
                actionType: PaymentActionType.Rejected,
                previousStatus: previousStatus,
                newStatus: PaymentStatus.Rejected,
                performedByUser: currentUser,
                balanceBeforeAction: balanceAtRejection,
                balanceAfterAction: balanceAtRejection,
                comment: rejectionComment));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.CreateNotificationForUserAsync(
                payment.SalesRepId,
                NotificationType.PaymentRejected,
                "Payment rejected",
                $"Your payment '{payment.PaymentNumber}' for customer '{payment.Customer.Name}' was rejected. Reason: {payment.RejectionReason}",
                payment.Id);

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.PaymentRejected,
                nameof(Payment),
                payment.Id,
                $"Payment '{payment.PaymentNumber}' for customer '{payment.Customer.Name}' was rejected by '{currentUser.FullName}'. {rejectionComment}");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            return new PaymentWorkflowResult
            {
                Status = PaymentWorkflowStatus.ConcurrencyConflict
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var updatedPayment = await LoadPaymentAsync(paymentId);

        return new PaymentWorkflowResult
        {
            Status = PaymentWorkflowStatus.Success,
            Payment = updatedPayment,
            CurrentBalance = balanceAtRejection ?? 0m
        };
    }

    public async Task<PaymentWorkflowResult> ReverseApprovedAsync(Guid paymentId, ReversePaymentRequestDto request, AppUser currentUser)
    {
        return await ReverseApprovedInternalAsync(
            paymentId,
            request.RowVersion,
            request.Reason,
            currentUser,
            useLegacyVoidTerminology: false);
    }

    public async Task<PaymentWorkflowResult> VoidApprovedAsync(Guid paymentId, VoidApprovedPaymentRequestDto request, AppUser currentUser)
    {
        return await ReverseApprovedInternalAsync(
            paymentId,
            request.RowVersion,
            request.Reason,
            currentUser,
            useLegacyVoidTerminology: true);
    }

    private async Task<PaymentWorkflowResult> ReverseApprovedInternalAsync(
        Guid paymentId,
        string rowVersionToken,
        string? reason,
        AppUser currentUser,
        bool useLegacyVoidTerminology)
    {
        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
        {
            return new PaymentWorkflowResult
            {
                Status = useLegacyVoidTerminology
                    ? PaymentWorkflowStatus.VoidReasonRequired
                    : PaymentWorkflowStatus.ReverseReasonRequired
            };
        }

        var payment = await LoadPaymentAsync(paymentId);
        if (payment is null)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentNotFound };

        if (!RowVersionTokenHelper.TryDecode(rowVersionToken, out var originalRowVersion))
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.InvalidConcurrencyToken };

        if (payment.Status == PaymentStatus.Reversed)
        {
            return new PaymentWorkflowResult
            {
                Status = useLegacyVoidTerminology
                    ? PaymentWorkflowStatus.PaymentAlreadyVoided
                    : PaymentWorkflowStatus.PaymentAlreadyReversed,
                Payment = payment
            };
        }

        if (payment.Status == PaymentStatus.Rejected)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentAlreadyRejected, Payment = payment };

        if (payment.Status != PaymentStatus.Approved)
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentNotApproved, Payment = payment };

        _context.Entry(payment).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

        decimal balanceBeforeReversal = 0m;
        decimal balanceAfterReversal = 0m;
        var previousStatus = payment.Status;
        var transitionVerb = useLegacyVoidTerminology ? "voided" : "reversed";
        var reasonPrefix = useLegacyVoidTerminology ? "Void reason" : "Reverse reason";
        var transitionTitle = useLegacyVoidTerminology ? "Approved payment voided" : "Approved payment reversed";
        var transitionComment = $"{reasonPrefix}: {normalizedReason}";
        var decisionTimeUtc = DateTime.UtcNow;
        var actionType = useLegacyVoidTerminology ? PaymentActionType.Voided : PaymentActionType.Reversed;
        var newStatus = useLegacyVoidTerminology ? PaymentStatus.Voided : PaymentStatus.Reversed;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var customerLockAcquired = await _customerFinancialLockService.LockAsync(payment.CustomerId);
            if (!customerLockAcquired)
            {
                await transaction.RollbackAsync();
                return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.CustomerNotFound };
            }

            payment = await LoadPaymentAsync(paymentId);
            if (payment is null)
            {
                await transaction.RollbackAsync();
                return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentNotFound };
            }

            if (payment.Status == PaymentStatus.Reversed)
            {
                await transaction.RollbackAsync();
                return new PaymentWorkflowResult
                {
                    Status = useLegacyVoidTerminology
                        ? PaymentWorkflowStatus.PaymentAlreadyVoided
                        : PaymentWorkflowStatus.PaymentAlreadyReversed,
                    Payment = payment
                };
            }

            if (payment.Status == PaymentStatus.Rejected)
            {
                await transaction.RollbackAsync();
                return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentAlreadyRejected, Payment = payment };
            }

            if (payment.Status != PaymentStatus.Approved)
            {
                await transaction.RollbackAsync();
                return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.PaymentNotApproved, Payment = payment };
            }

            _context.Entry(payment).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

            var balanceSnapshot = await _customerBalanceService.GetSnapshotAsync(payment.CustomerId);
            if (balanceSnapshot is null)
            {
                await transaction.RollbackAsync();
                return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.CustomerNotFound };
            }

            balanceBeforeReversal = balanceSnapshot.CurrentBalance;
            balanceAfterReversal = balanceBeforeReversal + payment.Amount;
            previousStatus = payment.Status;

            payment.Status = newStatus;
            payment.ReviewedByUserId = currentUser.Id;
            payment.ReviewedAtUtc = decisionTimeUtc;
            payment.RejectionReason = transitionComment;
            payment.UpdatedAtUtc = decisionTimeUtc;

            _context.PaymentActionHistories.Add(CreateHistoryEntry(
                paymentId: payment.Id,
                actionType: actionType,
                previousStatus: previousStatus,
                newStatus: newStatus,
                performedByUser: currentUser,
                balanceBeforeAction: balanceBeforeReversal,
                balanceAfterAction: balanceAfterReversal,
                comment: transitionComment));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _workflowSideEffectService.CreateNotificationForUserAsync(
                payment.SalesRepId,
                NotificationType.PaymentReversed,
                transitionTitle,
                $"Your approved payment '{payment.PaymentNumber}' for customer '{payment.Customer.Name}' was {transitionVerb}. Reason: {normalizedReason}",
                payment.Id);

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.PaymentReversed,
                nameof(Payment),
                payment.Id,
                $"Approved payment '{payment.PaymentNumber}' for customer '{payment.Customer.Name}' was {transitionVerb} by '{currentUser.FullName}'. {transitionComment}");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return new PaymentWorkflowResult { Status = PaymentWorkflowStatus.ConcurrencyConflict };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var updatedPayment = await LoadPaymentAsync(paymentId);

        return new PaymentWorkflowResult
        {
            Status = PaymentWorkflowStatus.Success,
            Payment = updatedPayment,
            CurrentBalance = balanceAfterReversal
        };
    }

    private async Task<ApprovalDecisionSignals> BuildApprovalDecisionSignalsAsync(Payment payment, decimal currentOutstandingBalance)
    {
        var pendingPaymentsForCustomerCount = await _context.Payments
            .AsNoTracking()
            .CountAsync(x => x.CustomerId == payment.CustomerId && x.Status == PaymentStatus.Pending);

        var duplicatePendingReferenceCount = 0;
        var normalizedReference = PaymentReferenceNormalizer.Normalize(payment.Reference);

        if (normalizedReference is not null)
        {
            var pendingReferences = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.CustomerId == payment.CustomerId &&
                    x.Status == PaymentStatus.Pending &&
                    x.Reference != null)
                .Select(x => x.Reference)
                .ToListAsync();

            duplicatePendingReferenceCount = pendingReferences
                .Count(reference => PaymentReferenceNormalizer.Normalize(reference) == normalizedReference);
        }

        var pendingForHours = (DateTime.UtcNow - payment.CreatedAtUtc).TotalHours;

        return new ApprovalDecisionSignals
        {
            IsStale = pendingForHours >= ApprovalStaleThresholdHours,
            HasHighBalanceImpact = currentOutstandingBalance > 0 &&
                                   payment.Amount / currentOutstandingBalance >= HighBalanceImpactRatioThreshold,
            HasMultiplePendingPayments = pendingPaymentsForCustomerCount > 1,
            HasDuplicatePendingReference = duplicatePendingReferenceCount > 1,
            MissingReferenceForNonCash = payment.PaymentMethod != PaymentMethod.Cash &&
                                         string.IsNullOrWhiteSpace(payment.Reference)
        };
    }

    private static string BuildApprovalHistoryComment(string reviewComment, ApprovalDecisionSignals signals)
    {
        var acknowledgedSignals = new List<string>();

        if (signals.IsStale)
            acknowledgedSignals.Add("Stale pending payment");

        if (signals.HasHighBalanceImpact)
            acknowledgedSignals.Add("High balance impact");

        if (signals.HasMultiplePendingPayments)
            acknowledgedSignals.Add("Multiple pending payments for customer");

        if (signals.HasDuplicatePendingReference)
            acknowledgedSignals.Add("Duplicate pending reference");

        return acknowledgedSignals.Count == 0
            ? $"Review comment: {reviewComment}"
            : $"Review comment: {reviewComment} | Acknowledged signals: {string.Join(", ", acknowledgedSignals)}";
    }

    private static string BuildRejectionComment(PaymentRejectionCategory category, string reason)
    {
        return $"Category: {category} | Reason: {reason}";
    }

    private static bool IsEquivalentOrBothMissing(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
            return true;

        return PaymentReferenceNormalizer.AreEquivalent(left, right);
    }

    private PaymentActionHistory CreateHistoryEntry(
        Guid paymentId,
        PaymentActionType actionType,
        PaymentStatus? previousStatus,
        PaymentStatus newStatus,
        AppUser performedByUser,
        decimal? balanceBeforeAction,
        decimal? balanceAfterAction,
        string? comment)
    {
        return new PaymentActionHistory
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            ActionType = actionType,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            PerformedByUserId = performedByUser.Id,
            PerformedByUserFullName = performedByUser.FullName,
            BalanceBeforeAction = balanceBeforeAction,
            BalanceAfterAction = balanceAfterAction,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            ActionAtUtc = DateTime.UtcNow
        };
    }

    private Task<Payment?> LoadPaymentAsync(Guid paymentId)
    {
        return _context.Payments
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .Include(x => x.ReviewedByUser)
            .FirstOrDefaultAsync(x => x.Id == paymentId);
    }

    private sealed class ApprovalDecisionSignals
    {
        public bool IsStale { get; init; }
        public bool HasHighBalanceImpact { get; init; }
        public bool HasMultiplePendingPayments { get; init; }
        public bool HasDuplicatePendingReference { get; init; }
        public bool MissingReferenceForNonCash { get; init; }
    }
}
