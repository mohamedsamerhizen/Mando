using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mando.Api.Configurations;
using Mando.Api.Data;
using Mando.Api.DTOs.Visits;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Visits;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Interfaces.Users;
using Mando.Api.Models.Visits;

namespace Mando.Api.Services.Visits;

public class VisitWorkflowService : IVisitWorkflowService
{
    private readonly AppDbContext _context;
    private readonly GpsSettings _gpsSettings;
    private readonly IWorkflowSideEffectService _workflowSideEffectService;
    private readonly ICustomerFinancialLockService _customerFinancialLockService;
    private readonly IUserStatusLockService _userStatusLockService;
    private readonly IVisitLifecycleLockService _visitLifecycleLockService;

    public VisitWorkflowService(
        AppDbContext context,
        IOptions<GpsSettings> gpsOptions,
        IWorkflowSideEffectService workflowSideEffectService,
        ICustomerFinancialLockService customerFinancialLockService,
        IUserStatusLockService userStatusLockService,
        IVisitLifecycleLockService visitLifecycleLockService)
    {
        _context = context;
        _gpsSettings = gpsOptions.Value;
        _workflowSideEffectService = workflowSideEffectService;
        _customerFinancialLockService = customerFinancialLockService;
        _userStatusLockService = userStatusLockService;
        _visitLifecycleLockService = visitLifecycleLockService;
    }

    public async Task<VisitWorkflowResult> StartAsync(StartVisitRequestDto request, AppUser currentUser)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.Id == request.CustomerId);
        if (customer is null)
            return new VisitWorkflowResult { Status = VisitWorkflowStatus.CustomerNotFound };

        var distance = GeoHelper.CalculateDistanceInMeters(
            request.Latitude,
            request.Longitude,
            customer.Latitude,
            customer.Longitude);

        if (customer.AssignedSalesRepId != currentUser.Id)
        {
            await LogVisitAttemptAsync(
                customer.Id,
                currentUser,
                request,
                distance,
                ResolveComplianceStatus(request.AccuracyInMeters, distance),
                false,
                "Customer is assigned to another sales rep.");

            return new VisitWorkflowResult { Status = VisitWorkflowStatus.Forbidden };
        }

        if (customer.Status != CustomerStatus.Active)
        {
            await LogVisitAttemptAsync(
                customer.Id,
                currentUser,
                request,
                distance,
                ResolveComplianceStatus(request.AccuracyInMeters, distance),
                false,
                "Customer is inactive.");

            return new VisitWorkflowResult { Status = VisitWorkflowStatus.CustomerInactive };
        }

        if ((double)request.AccuracyInMeters > _gpsSettings.MaxAllowedAccuracyMeters)
        {
            await LogVisitAttemptAsync(
                customer.Id,
                currentUser,
                request,
                distance,
                VisitComplianceStatus.WeakAccuracy,
                false,
                $"Location accuracy is too weak. Max allowed accuracy is {_gpsSettings.MaxAllowedAccuracyMeters:0.##} meters.");

            return new VisitWorkflowResult
            {
                Status = VisitWorkflowStatus.WeakLocationAccuracy,
                MaxAllowedAccuracyMeters = _gpsSettings.MaxAllowedAccuracyMeters,
                MaxStartVisitDistanceMeters = _gpsSettings.MaxStartVisitDistanceMeters,
                MaxEndVisitDistanceMeters = _gpsSettings.MaxEndVisitDistanceMeters
            };
        }

        if (distance > (double)_gpsSettings.MaxStartVisitDistanceMeters)
        {
            await LogVisitAttemptAsync(
                customer.Id,
                currentUser,
                request,
                distance,
                VisitComplianceStatus.OutOfRange,
                false,
                $"Visit start attempt is out of range. Distance is {distance:0.##} meters and max allowed distance is {_gpsSettings.MaxStartVisitDistanceMeters:0.##} meters.");

            return new VisitWorkflowResult
            {
                Status = VisitWorkflowStatus.OutOfRange,
                DistanceFromCustomerInMeters = distance,
                MaxAllowedAccuracyMeters = _gpsSettings.MaxAllowedAccuracyMeters,
                MaxStartVisitDistanceMeters = _gpsSettings.MaxStartVisitDistanceMeters
            };
        }

        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var salesRepLockAcquired = await _userStatusLockService.LockAsync(currentUser.Id);
            if (!salesRepLockAcquired)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.Forbidden };
            }

            var customerLockAcquired = await _customerFinancialLockService.LockAsync(request.CustomerId);
            if (!customerLockAcquired)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.CustomerNotFound };
            }

            var lockedSalesRep = await _context.Users.FirstOrDefaultAsync(x => x.Id == currentUser.Id);
            if (lockedSalesRep is null || !lockedSalesRep.IsActive)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.Forbidden };
            }

            var lockedCustomer = await _context.Customers.FirstOrDefaultAsync(x => x.Id == request.CustomerId);
            if (lockedCustomer is null)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.CustomerNotFound };
            }

            if (lockedCustomer.AssignedSalesRepId != currentUser.Id)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.Forbidden };
            }

            if (lockedCustomer.Status != CustomerStatus.Active)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.CustomerInactive };
            }

            var hasActiveVisit = await _context.Visits.AnyAsync(x =>
                x.SalesRepId == currentUser.Id &&
                x.Status == VisitStatus.InProgress);

            if (hasActiveVisit)
            {
                await transaction.RollbackAsync();

                return new VisitWorkflowResult { Status = VisitWorkflowStatus.ActiveVisitExists };
            }

            var visit = new Visit
            {
                Id = Guid.NewGuid(),
                CustomerId = lockedCustomer.Id,
                SalesRepId = lockedSalesRep.Id,
                CheckInAtUtc = now,
                CheckInLatitude = request.Latitude,
                CheckInLongitude = request.Longitude,
                CheckInAccuracyInMeters = request.AccuracyInMeters,
                DistanceFromCustomerInMeters = distance,
                Status = VisitStatus.InProgress,
                Outcome = VisitOutcome.Pending,
                Notes = NormalizeOptionalText(request.Notes),
                CreatedAtUtc = now,
                Customer = lockedCustomer,
                SalesRep = lockedSalesRep
            };

            _context.Visits.Add(visit);

            _context.VisitActionHistories.Add(CreateActionHistory(
                visitId: visit.Id,
                actionType: VisitActionType.Started,
                previousStatus: null,
                newStatus: visit.Status,
                previousOutcome: null,
                newOutcome: visit.Outcome,
                performedByUser: currentUser,
                comment: "Visit started."));

            _context.VisitAttemptLogs.Add(CreateAttemptLog(
                customerId: lockedCustomer.Id,
                currentUser: currentUser,
                request: request,
                distanceFromCustomerInMeters: distance,
                complianceStatus: VisitComplianceStatus.Compliant,
                isSuccessful: true,
                reason: "Visit started successfully."));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.VisitStarted,
                nameof(Visit),
                visit.Id,
                $"Visit started by '{currentUser.FullName}' for customer '{lockedCustomer.Name}'.");

            return new VisitWorkflowResult
            {
                Status = VisitWorkflowStatus.Success,
                Visit = visit,
                MaxAllowedAccuracyMeters = _gpsSettings.MaxAllowedAccuracyMeters,
                MaxStartVisitDistanceMeters = _gpsSettings.MaxStartVisitDistanceMeters
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<VisitWorkflowResult> EndAsync(Guid visitId, EndVisitRequestDto request, AppUser currentUser)
    {
        if ((double)request.AccuracyInMeters > _gpsSettings.MaxAllowedAccuracyMeters)
        {
            return new VisitWorkflowResult
            {
                Status = VisitWorkflowStatus.WeakLocationAccuracy,
                MaxAllowedAccuracyMeters = _gpsSettings.MaxAllowedAccuracyMeters,
                MaxStartVisitDistanceMeters = _gpsSettings.MaxStartVisitDistanceMeters,
                MaxEndVisitDistanceMeters = _gpsSettings.MaxEndVisitDistanceMeters
            };
        }

        var visit = await LoadVisitAsync(visitId);
        if (visit is null)
            return new VisitWorkflowResult { Status = VisitWorkflowStatus.VisitNotFound };

        if (!RowVersionTokenHelper.TryDecode(request.RowVersion, out var originalRowVersion))
            return new VisitWorkflowResult { Status = VisitWorkflowStatus.InvalidConcurrencyToken };

        if (visit.SalesRepId != currentUser.Id)
            return new VisitWorkflowResult { Status = VisitWorkflowStatus.Forbidden };

        if (request.Outcome is VisitOutcome.Pending or VisitOutcome.Cancelled)
            return new VisitWorkflowResult { Status = VisitWorkflowStatus.InvalidOutcome, Visit = visit };

        var normalizedNotes = NormalizeOptionalText(request.Notes);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var visitLockAcquired = await _visitLifecycleLockService.LockAsync(visitId);
            if (!visitLockAcquired)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.VisitNotFound };
            }

            visit = await LoadVisitAsync(visitId);
            if (visit is null)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.VisitNotFound };
            }

            if (visit.SalesRepId != currentUser.Id)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.Forbidden };
            }

            if (visit.Status != VisitStatus.InProgress)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.VisitNotInProgress, Visit = visit };
            }

            var checkoutDistance = GeoHelper.CalculateDistanceInMeters(
                request.Latitude,
                request.Longitude,
                visit.Customer.Latitude,
                visit.Customer.Longitude);

            if (checkoutDistance > _gpsSettings.MaxEndVisitDistanceMeters)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult
                {
                    Status = VisitWorkflowStatus.OutOfRange,
                    Visit = visit,
                    DistanceFromCustomerInMeters = checkoutDistance,
                    MaxAllowedAccuracyMeters = _gpsSettings.MaxAllowedAccuracyMeters,
                    MaxStartVisitDistanceMeters = _gpsSettings.MaxStartVisitDistanceMeters,
                    MaxEndVisitDistanceMeters = _gpsSettings.MaxEndVisitDistanceMeters
                };
            }

            _context.Entry(visit).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

            var previousStatus = visit.Status;
            var previousOutcome = visit.Outcome;

            visit.CheckOutAtUtc = DateTime.UtcNow;
            visit.CheckOutLatitude = request.Latitude;
            visit.CheckOutLongitude = request.Longitude;
            visit.CheckOutAccuracyInMeters = request.AccuracyInMeters;
            visit.DistanceFromCustomerInMeters = checkoutDistance;
            visit.Status = VisitStatus.Completed;
            visit.Outcome = request.Outcome;

            if (!string.IsNullOrWhiteSpace(normalizedNotes))
            {
                visit.Notes = normalizedNotes;
            }

            visit.UpdatedAtUtc = DateTime.UtcNow;

            _context.VisitActionHistories.Add(CreateActionHistory(
                visitId: visit.Id,
                actionType: VisitActionType.Completed,
                previousStatus: previousStatus,
                newStatus: visit.Status,
                previousOutcome: previousOutcome,
                newOutcome: visit.Outcome,
                performedByUser: currentUser,
                comment: BuildVisitCompletionComment(visit.Outcome, normalizedNotes)));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.VisitCompleted,
                nameof(Visit),
                visit.Id,
                $"Visit '{visit.Id}' for customer '{visit.Customer.Name}' was completed by '{currentUser.FullName}' with outcome '{visit.Outcome}'.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            return new VisitWorkflowResult
            {
                Status = VisitWorkflowStatus.ConcurrencyConflict
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return new VisitWorkflowResult
        {
            Status = VisitWorkflowStatus.Success,
            Visit = visit,
            MaxAllowedAccuracyMeters = _gpsSettings.MaxAllowedAccuracyMeters,
            MaxStartVisitDistanceMeters = _gpsSettings.MaxStartVisitDistanceMeters,
            MaxEndVisitDistanceMeters = _gpsSettings.MaxEndVisitDistanceMeters,
            DistanceFromCustomerInMeters = visit.DistanceFromCustomerInMeters
        };
    }

    public async Task<VisitWorkflowResult> CancelAsync(Guid visitId, CancelVisitRequestDto request, AppUser currentUser)
    {
        var visit = await LoadVisitAsync(visitId);
        if (visit is null)
            return new VisitWorkflowResult { Status = VisitWorkflowStatus.VisitNotFound };

        if (!RowVersionTokenHelper.TryDecode(request.RowVersion, out var originalRowVersion))
            return new VisitWorkflowResult { Status = VisitWorkflowStatus.InvalidConcurrencyToken };

        if (visit.SalesRepId != currentUser.Id)
            return new VisitWorkflowResult { Status = VisitWorkflowStatus.Forbidden };

        var normalizedNotes = NormalizeOptionalText(request.Notes);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var visitLockAcquired = await _visitLifecycleLockService.LockAsync(visitId);
            if (!visitLockAcquired)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.VisitNotFound };
            }

            visit = await LoadVisitAsync(visitId);
            if (visit is null)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.VisitNotFound };
            }

            if (visit.SalesRepId != currentUser.Id)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.Forbidden };
            }

            if (visit.Status != VisitStatus.InProgress)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.VisitNotInProgress, Visit = visit };
            }

            var hasOrders = await _context.Orders.AnyAsync(x => x.VisitId == visitId);
            if (hasOrders)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.VisitHasOrders, Visit = visit };
            }

            var hasPayments = await _context.Payments.AnyAsync(x => x.VisitId == visitId);
            if (hasPayments)
            {
                await transaction.RollbackAsync();
                return new VisitWorkflowResult { Status = VisitWorkflowStatus.VisitHasPayments, Visit = visit };
            }

            _context.Entry(visit).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

            var previousStatus = visit.Status;
            var previousOutcome = visit.Outcome;

            visit.Status = VisitStatus.Cancelled;
            visit.Outcome = VisitOutcome.Cancelled;
            visit.CheckOutAtUtc = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(normalizedNotes))
            {
                visit.Notes = normalizedNotes;
            }

            visit.UpdatedAtUtc = DateTime.UtcNow;

            _context.VisitActionHistories.Add(CreateActionHistory(
                visitId: visit.Id,
                actionType: VisitActionType.Cancelled,
                previousStatus: previousStatus,
                newStatus: visit.Status,
                previousOutcome: previousOutcome,
                newOutcome: visit.Outcome,
                performedByUser: currentUser,
                comment: BuildVisitCancellationComment(normalizedNotes)));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.VisitCancelled,
                nameof(Visit),
                visit.Id,
                $"Visit '{visit.Id}' for customer '{visit.Customer.Name}' was cancelled by '{currentUser.FullName}'.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            return new VisitWorkflowResult
            {
                Status = VisitWorkflowStatus.ConcurrencyConflict
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return new VisitWorkflowResult
        {
            Status = VisitWorkflowStatus.Success,
            Visit = visit,
            MaxAllowedAccuracyMeters = _gpsSettings.MaxAllowedAccuracyMeters,
            MaxStartVisitDistanceMeters = _gpsSettings.MaxStartVisitDistanceMeters
        };
    }

    private Task<Visit?> LoadVisitAsync(Guid visitId)
    {
        return _context.Visits
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .FirstOrDefaultAsync(x => x.Id == visitId);
    }

    private async Task LogVisitAttemptAsync(
        Guid customerId,
        AppUser currentUser,
        StartVisitRequestDto request,
        double distanceFromCustomerInMeters,
        VisitComplianceStatus complianceStatus,
        bool isSuccessful,
        string reason)
    {
        _context.VisitAttemptLogs.Add(CreateAttemptLog(
            customerId,
            currentUser,
            request,
            distanceFromCustomerInMeters,
            complianceStatus,
            isSuccessful,
            reason));

        await _context.SaveChangesAsync();
    }

    private static VisitAttemptLog CreateAttemptLog(
        Guid customerId,
        AppUser currentUser,
        StartVisitRequestDto request,
        double distanceFromCustomerInMeters,
        VisitComplianceStatus complianceStatus,
        bool isSuccessful,
        string reason)
    {
        return new VisitAttemptLog
        {
            Id = Guid.NewGuid(),
            SalesRepId = currentUser.Id,
            CustomerId = customerId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AccuracyInMeters = request.AccuracyInMeters,
            DistanceFromCustomerInMeters = distanceFromCustomerInMeters,
            ComplianceStatus = complianceStatus,
            IsSuccessful = isSuccessful,
            Reason = reason.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static VisitActionHistory CreateActionHistory(
        Guid visitId,
        VisitActionType actionType,
        VisitStatus? previousStatus,
        VisitStatus newStatus,
        VisitOutcome? previousOutcome,
        VisitOutcome newOutcome,
        AppUser performedByUser,
        string? comment)
    {
        return new VisitActionHistory
        {
            Id = Guid.NewGuid(),
            VisitId = visitId,
            ActionType = actionType,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            PreviousOutcome = previousOutcome,
            NewOutcome = newOutcome,
            PerformedByUserId = performedByUser.Id,
            PerformedByUserFullName = performedByUser.FullName,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            ActionAtUtc = DateTime.UtcNow
        };
    }

    private VisitComplianceStatus ResolveComplianceStatus(decimal accuracyInMeters, double distanceFromCustomerInMeters)
    {
        if ((double)accuracyInMeters > _gpsSettings.MaxAllowedAccuracyMeters)
            return VisitComplianceStatus.WeakAccuracy;

        if (distanceFromCustomerInMeters > _gpsSettings.MaxStartVisitDistanceMeters)
            return VisitComplianceStatus.OutOfRange;

        return VisitComplianceStatus.Compliant;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildVisitCompletionComment(VisitOutcome outcome, string? notes)
    {
        return string.IsNullOrWhiteSpace(notes)
            ? $"Visit completed with outcome '{outcome}'."
            : $"Visit completed with outcome '{outcome}'. Notes: {notes}";
    }

    private static string BuildVisitCancellationComment(string? notes)
    {
        return string.IsNullOrWhiteSpace(notes)
            ? "Visit cancelled."
            : $"Visit cancelled. Notes: {notes}";
    }
}