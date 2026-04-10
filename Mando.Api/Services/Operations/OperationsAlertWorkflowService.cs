using Microsoft.EntityFrameworkCore;
using Mando.Api.Data;
using Mando.Api.DTOs.Operations;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Interfaces.Operations;
using Mando.Api.Models.Operations;

namespace Mando.Api.Services.Operations;

public class OperationsAlertWorkflowService : IOperationsAlertWorkflowService
{
    private const decimal HighBalanceImpactRatioThreshold = 0.80m;
    private const int DefaultPaymentStaleAfterHours = 24;
    private const int DefaultOrderStaleAfterHours = 24;
    private const int DefaultVisitStaleAfterHours = 8;
    private const decimal DefaultNearCreditLimitRatio = 0.90m;

    private readonly AppDbContext _context;
    private readonly ICustomerBalanceService _customerBalanceService;

    public OperationsAlertWorkflowService(
        AppDbContext context,
        ICustomerBalanceService customerBalanceService)
    {
        _context = context;
        _customerBalanceService = customerBalanceService;
    }

    public async Task<OperationsAlertReviewWorkflowResult> ReviewAsync(
        ReviewOperationsAlertRequestDto request,
        AppUser currentUser)
    {
        if (string.IsNullOrWhiteSpace(request.AlertFingerprint))
        {
            return new OperationsAlertReviewWorkflowResult
            {
                Status = OperationsAlertReviewWorkflowStatus.AlertFingerprintRequired
            };
        }

        if (!OperationsAlertIdentityHelper.TryParseFingerprint(request.AlertFingerprint, out var parsedFingerprint))
        {
            return new OperationsAlertReviewWorkflowResult
            {
                Status = OperationsAlertReviewWorkflowStatus.InvalidAlertFingerprint
            };
        }

        if (request.Status == OperationsAlertReviewStatus.Open)
        {
            return new OperationsAlertReviewWorkflowResult
            {
                Status = OperationsAlertReviewWorkflowStatus.InvalidReviewStatus
            };
        }

        if ((request.Status == OperationsAlertReviewStatus.Resolved || request.Status == OperationsAlertReviewStatus.Dismissed) &&
            string.IsNullOrWhiteSpace(request.Comment))
        {
            return new OperationsAlertReviewWorkflowResult
            {
                Status = OperationsAlertReviewWorkflowStatus.ReviewCommentRequired
            };
        }

        var latestReview = await _context.OperationsAlertReviews
            .AsNoTracking()
            .Where(x => x.AlertFingerprint == parsedFingerprint.AlertFingerprint)
            .OrderByDescending(x => x.ReviewedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (latestReview is not null)
        {
            if (latestReview.Status is OperationsAlertReviewStatus.Resolved or OperationsAlertReviewStatus.Dismissed)
            {
                return new OperationsAlertReviewWorkflowResult
                {
                    Status = OperationsAlertReviewWorkflowStatus.AlertAlreadyClosed
                };
            }

            if (latestReview.Status == request.Status)
            {
                return new OperationsAlertReviewWorkflowResult
                {
                    Status = OperationsAlertReviewWorkflowStatus.AlertAlreadyInRequestedState
                };
            }
        }

        var currentAlertSnapshot = await LoadCurrentAlertSnapshotAsync(parsedFingerprint);

        var canResolveAcknowledgedMissingAlert =
            request.Status == OperationsAlertReviewStatus.Resolved &&
            latestReview?.Status == OperationsAlertReviewStatus.Acknowledged &&
            currentAlertSnapshot is null;

        if (currentAlertSnapshot is null && !canResolveAcknowledgedMissingAlert)
        {
            return new OperationsAlertReviewWorkflowResult
            {
                Status = OperationsAlertReviewWorkflowStatus.AlertNotFound
            };
        }

        var shortReasonSnapshot = currentAlertSnapshot?.ShortReason ?? latestReview?.ShortReasonSnapshot ?? string.Empty;
        var triggeredAtUtc = currentAlertSnapshot?.TriggeredAtUtc ?? latestReview?.TriggeredAtUtc ?? parsedFingerprint.TriggeredAtUtc;

        var review = new OperationsAlertReview
        {
            AlertKey = parsedFingerprint.AlertKey,
            AlertFingerprint = parsedFingerprint.AlertFingerprint,
            Category = parsedFingerprint.Category,
            EntityType = parsedFingerprint.EntityType,
            EntityId = parsedFingerprint.EntityId,
            TriggeredAtUtc = triggeredAtUtc,
            ShortReasonSnapshot = shortReasonSnapshot,
            Status = request.Status,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            ReviewedByUserId = currentUser.Id,
            ReviewedByUserFullName = currentUser.FullName,
            ReviewedAtUtc = DateTime.UtcNow
        };

        _context.OperationsAlertReviews.Add(review);
        await _context.SaveChangesAsync();

        return new OperationsAlertReviewWorkflowResult
        {
            Status = OperationsAlertReviewWorkflowStatus.Success,
            Review = MapReview(review)
        };
    }

    private async Task<CurrentAlertSnapshot?> LoadCurrentAlertSnapshotAsync(
        OperationsAlertIdentityHelper.ParsedOperationsAlertFingerprint parsed)
    {
        return parsed.Category switch
        {
            OperationsAlertCategory.PaymentStalePending => await GetPendingPaymentSnapshotAsync(parsed.EntityId, parsed.TriggeredAtUtc, snapshot =>
            {
                var ageInHours = (DateTime.UtcNow - snapshot.CreatedAtUtc).TotalHours;
                return ageInHours >= DefaultPaymentStaleAfterHours
                    ? new CurrentAlertSnapshot(snapshot.CreatedAtUtc, $"Pending payment has been waiting {Math.Round(ageInHours, 2):0.##}h without a decision.")
                    : null;
            }),

            OperationsAlertCategory.PaymentApprovalBlocked => await GetPendingPaymentSnapshotAsync(parsed.EntityId, parsed.TriggeredAtUtc, async snapshot =>
            {
                var balance = await _customerBalanceService.GetSnapshotAsync(snapshot.CustomerId);
                if (balance is null)
                    return null;

                return balance.CurrentBalance <= 0 || snapshot.Amount > balance.CurrentBalance
                    ? new CurrentAlertSnapshot(
                        snapshot.CreatedAtUtc,
                        balance.CurrentBalance <= 0
                            ? "Payment cannot be approved because the customer has no outstanding balance."
                            : $"Payment amount exceeds current outstanding balance ({balance.CurrentBalance:0.00}).")
                    : null;
            }),

            OperationsAlertCategory.PaymentMissingReference => await GetPendingPaymentSnapshotAsync(parsed.EntityId, parsed.TriggeredAtUtc, snapshot =>
            {
                return snapshot.PaymentMethod != PaymentMethod.Cash && string.IsNullOrWhiteSpace(snapshot.Reference)
                    ? new CurrentAlertSnapshot(snapshot.CreatedAtUtc, "Non-cash payment is missing a reference.")
                    : null;
            }),

            OperationsAlertCategory.PaymentHighBalanceImpact => await GetPendingPaymentSnapshotAsync(parsed.EntityId, parsed.TriggeredAtUtc, async snapshot =>
            {
                var balance = await _customerBalanceService.GetSnapshotAsync(snapshot.CustomerId);
                if (balance is null || balance.CurrentBalance <= 0)
                    return null;

                return snapshot.Amount / balance.CurrentBalance >= HighBalanceImpactRatioThreshold
                    ? new CurrentAlertSnapshot(snapshot.CreatedAtUtc, "Payment amount covers a very large portion of the customer balance.")
                    : null;
            }),

            OperationsAlertCategory.PaymentDuplicateReference => await GetDuplicateReferenceSnapshotAsync(parsed),
            OperationsAlertCategory.PaymentMultiplePending => await GetMultiplePendingSnapshotAsync(parsed),
            OperationsAlertCategory.OrderStaleActive => await GetOrderSnapshotAsync(parsed),
            OperationsAlertCategory.VisitStaleInProgress => await GetVisitSnapshotAsync(parsed),
            OperationsAlertCategory.CustomerNearCreditLimit => await GetCustomerNearLimitSnapshotAsync(parsed),
            OperationsAlertCategory.CustomerOverCreditLimit => await GetCustomerOverLimitSnapshotAsync(parsed),
            _ => null
        };
    }

    private async Task<CurrentAlertSnapshot?> GetPendingPaymentSnapshotAsync(
        Guid paymentId,
        DateTime triggeredAtUtc,
        Func<PendingPaymentSnapshot, CurrentAlertSnapshot?> evaluator)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .Where(x => x.Id == paymentId && x.Status == PaymentStatus.Pending)
            .Select(x => new PendingPaymentSnapshot
            {
                PaymentId = x.Id,
                CustomerId = x.CustomerId,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                Reference = x.Reference,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (payment is null || payment.CreatedAtUtc != triggeredAtUtc)
            return null;

        return evaluator(payment);
    }

    private async Task<CurrentAlertSnapshot?> GetPendingPaymentSnapshotAsync(
        Guid paymentId,
        DateTime triggeredAtUtc,
        Func<PendingPaymentSnapshot, Task<CurrentAlertSnapshot?>> evaluator)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .Where(x => x.Id == paymentId && x.Status == PaymentStatus.Pending)
            .Select(x => new PendingPaymentSnapshot
            {
                PaymentId = x.Id,
                CustomerId = x.CustomerId,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                Reference = x.Reference,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (payment is null || payment.CreatedAtUtc != triggeredAtUtc)
            return null;

        return await evaluator(payment);
    }

    private async Task<CurrentAlertSnapshot?> GetDuplicateReferenceSnapshotAsync(
        OperationsAlertIdentityHelper.ParsedOperationsAlertFingerprint parsed)
    {
        var normalizedQualifier = parsed.Qualifier;
        if (string.IsNullOrWhiteSpace(normalizedQualifier))
            return null;

        var group = await _context.Payments
            .AsNoTracking()
            .Where(x =>
                x.Status == PaymentStatus.Pending &&
                x.CustomerId == parsed.EntityId &&
                x.Reference != null)
            .Select(x => new
            {
                x.Reference,
                x.CreatedAtUtc
            })
            .ToListAsync();

        var duplicateGroup = group
            .Where(x => !string.IsNullOrWhiteSpace(x.Reference) &&
                        PaymentReferenceNormalizer.Normalize(x.Reference!) == normalizedQualifier)
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();

        if (duplicateGroup.Count <= 1)
            return null;

        var oldestCreatedAtUtc = duplicateGroup.First().CreatedAtUtc;
        if (oldestCreatedAtUtc != parsed.TriggeredAtUtc)
            return null;

        return new CurrentAlertSnapshot(
            oldestCreatedAtUtc,
            $"Reference '{normalizedQualifier}' appears on {duplicateGroup.Count} pending payments for the same customer.");
    }

    private async Task<CurrentAlertSnapshot?> GetMultiplePendingSnapshotAsync(
        OperationsAlertIdentityHelper.ParsedOperationsAlertFingerprint parsed)
    {
        var payments = await _context.Payments
            .AsNoTracking()
            .Where(x => x.Status == PaymentStatus.Pending && x.CustomerId == parsed.EntityId)
            .Select(x => x.CreatedAtUtc)
            .OrderBy(x => x)
            .ToListAsync();

        if (payments.Count <= 1)
            return null;

        var oldestCreatedAtUtc = payments.First();
        if (oldestCreatedAtUtc != parsed.TriggeredAtUtc)
            return null;

        return new CurrentAlertSnapshot(
            oldestCreatedAtUtc,
            $"Customer has {payments.Count} pending payments waiting for review.");
    }

    private async Task<CurrentAlertSnapshot?> GetOrderSnapshotAsync(
        OperationsAlertIdentityHelper.ParsedOperationsAlertFingerprint parsed)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(x => x.Id == parsed.EntityId && x.Status != OrderStatus.Cancelled)
            .Select(x => new { x.CreatedAtUtc })
            .FirstOrDefaultAsync();

        if (order is null || order.CreatedAtUtc != parsed.TriggeredAtUtc)
            return null;

        var ageInHours = (DateTime.UtcNow - order.CreatedAtUtc).TotalHours;
        return ageInHours >= DefaultOrderStaleAfterHours
            ? new CurrentAlertSnapshot(order.CreatedAtUtc, $"Active order has remained open for {Math.Round(ageInHours, 2):0.##}h.")
            : null;
    }

    private async Task<CurrentAlertSnapshot?> GetVisitSnapshotAsync(
        OperationsAlertIdentityHelper.ParsedOperationsAlertFingerprint parsed)
    {
        var visit = await _context.Visits
            .AsNoTracking()
            .Where(x => x.Id == parsed.EntityId && x.Status == VisitStatus.InProgress)
            .Select(x => new { x.CheckInAtUtc })
            .FirstOrDefaultAsync();

        if (visit is null || visit.CheckInAtUtc != parsed.TriggeredAtUtc)
            return null;

        var ageInHours = (DateTime.UtcNow - visit.CheckInAtUtc).TotalHours;
        return ageInHours >= DefaultVisitStaleAfterHours
            ? new CurrentAlertSnapshot(visit.CheckInAtUtc, $"Visit has been in progress for {Math.Round(ageInHours, 2):0.##}h without closure.")
            : null;
    }

    private async Task<CurrentAlertSnapshot?> GetCustomerNearLimitSnapshotAsync(
        OperationsAlertIdentityHelper.ParsedOperationsAlertFingerprint parsed)
    {
        var balance = await _customerBalanceService.GetSnapshotAsync(parsed.EntityId);
        if (balance is null || balance.CurrentBalance <= 0 || balance.CreditLimit <= 0)
            return null;

        var ratio = balance.CurrentBalance / balance.CreditLimit;
        if (ratio <= 1m && ratio >= DefaultNearCreditLimitRatio)
        {
            return new CurrentAlertSnapshot(
                parsed.TriggeredAtUtc,
                $"Customer balance has reached {(ratio * 100m):0.##}% of the credit limit.");
        }

        return null;
    }

    private async Task<CurrentAlertSnapshot?> GetCustomerOverLimitSnapshotAsync(
        OperationsAlertIdentityHelper.ParsedOperationsAlertFingerprint parsed)
    {
        var balance = await _customerBalanceService.GetSnapshotAsync(parsed.EntityId);
        if (balance is null || balance.CurrentBalance <= 0)
            return null;

        var isOverCreditLimit = balance.CreditLimit <= 0
            ? balance.CurrentBalance > 0
            : balance.CurrentBalance > balance.CreditLimit;

        if (!isOverCreditLimit)
            return null;

        var shortReason = balance.CreditLimit > 0
            ? $"Customer balance ({balance.CurrentBalance:0.00}) is above credit limit ({balance.CreditLimit:0.00})."
            : $"Customer balance ({balance.CurrentBalance:0.00}) exceeds a zero-credit account.";

        return new CurrentAlertSnapshot(parsed.TriggeredAtUtc, shortReason);
    }

    private static OperationsAlertReviewDto MapReview(OperationsAlertReview review)
    {
        return new OperationsAlertReviewDto
        {
            Id = review.Id,
            AlertKey = review.AlertKey,
            AlertFingerprint = review.AlertFingerprint,
            Category = review.Category,
            EntityType = review.EntityType,
            EntityId = review.EntityId,
            TriggeredAtUtc = review.TriggeredAtUtc,
            Status = review.Status,
            Comment = review.Comment,
            ReviewedByUserId = review.ReviewedByUserId,
            ReviewedByUserFullName = review.ReviewedByUserFullName,
            ReviewedAtUtc = review.ReviewedAtUtc
        };
    }

    private sealed record CurrentAlertSnapshot(DateTime TriggeredAtUtc, string ShortReason);

    private sealed class PendingPaymentSnapshot
    {
        public Guid PaymentId { get; init; }
        public Guid CustomerId { get; init; }
        public decimal Amount { get; init; }
        public PaymentMethod PaymentMethod { get; init; }
        public string? Reference { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}