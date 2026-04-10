using Microsoft.EntityFrameworkCore;
using Mando.Api.Common;
using Mando.Api.Data;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Payments;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Interfaces.Payments;
using Mando.Api.Models.Payments;

namespace Mando.Api.Services.Payments;

public class PaymentQueryService : IPaymentQueryService
{
    private const decimal HighBalanceImpactRatioThreshold = 0.80m;

    private readonly AppDbContext _context;
    private readonly ICustomerBalanceService _customerBalanceService;

    public PaymentQueryService(
        AppDbContext context,
        ICustomerBalanceService customerBalanceService)
    {
        _context = context;
        _customerBalanceService = customerBalanceService;
    }

    public async Task<PaymentQueryResult<PagedResultDto<PaymentResponseDto>>> GetAllAsync(
        GetPaymentsQueryDto query,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var isAdminOrManager =
            currentUserRoles.Contains(AppRoles.Admin) ||
            currentUserRoles.Contains(AppRoles.Manager);

        var paymentsQuery = _context.Payments
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .Include(x => x.ReviewedByUser)
            .AsQueryable();

        if (!isAdminOrManager)
        {
            paymentsQuery = paymentsQuery.Where(x => x.SalesRepId == currentUser.Id);
        }

        if (query.CustomerId.HasValue)
            paymentsQuery = paymentsQuery.Where(x => x.CustomerId == query.CustomerId.Value);

        if (query.SalesRepId.HasValue)
            paymentsQuery = paymentsQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);

        if (query.ReviewedByUserId.HasValue)
            paymentsQuery = paymentsQuery.Where(x => x.ReviewedByUserId == query.ReviewedByUserId.Value);

        if (query.PaymentMethod.HasValue)
            paymentsQuery = paymentsQuery.Where(x => x.PaymentMethod == query.PaymentMethod.Value);

        if (query.Status.HasValue)
            paymentsQuery = paymentsQuery.Where(x => x.Status == query.Status.Value);

        if (!string.IsNullOrWhiteSpace(query.PaymentNumber))
        {
            var paymentNumber = query.PaymentNumber.Trim();
            paymentsQuery = paymentsQuery.Where(x => x.PaymentNumber.Contains(paymentNumber));
        }

        if (!string.IsNullOrWhiteSpace(query.Reference))
        {
            var normalizedReference = PaymentReferenceNormalizer.Normalize(query.Reference);
            if (normalizedReference is null)
            {
                paymentsQuery = paymentsQuery.Where(_ => false);
            }
            else
            {
                var matchingPaymentIds = (await paymentsQuery
                    .Where(x => x.Reference != null)
                    .Select(x => new
                    {
                        x.Id,
                        x.Reference
                    })
                    .AsNoTracking()
                    .ToListAsync())
                    .Where(x => PaymentReferenceNormalizer.Normalize(x.Reference) == normalizedReference)
                    .Select(x => x.Id)
                    .ToList();

                paymentsQuery = matchingPaymentIds.Count == 0
                    ? paymentsQuery.Where(_ => false)
                    : paymentsQuery.Where(x => matchingPaymentIds.Contains(x.Id));
            }
        }

        var createdToUtc = NormalizeCreatedToUtc(query.DateToUtc);

        if (query.DateFromUtc.HasValue)
            paymentsQuery = paymentsQuery.Where(x => x.CreatedAtUtc >= query.DateFromUtc.Value);

        if (createdToUtc.HasValue)
            paymentsQuery = paymentsQuery.Where(x => x.CreatedAtUtc < createdToUtc.Value);

        var result = await paymentsQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsNoTracking()
            .Select(x => new PaymentResponseDto
            {
                Id = x.Id,
                PaymentNumber = x.PaymentNumber,
                VisitId = x.VisitId,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.Name,
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                Status = x.Status,
                Reference = x.Reference,
                Notes = x.Notes,
                ReviewedByUserId = x.ReviewedByUserId,
                ReviewedByUserName = x.ReviewedByUser != null ? x.ReviewedByUser.FullName : null,
                ReviewedAtUtc = x.ReviewedAtUtc,
                RejectionReason = x.RejectionReason,
                RowVersion = RowVersionTokenHelper.Encode(x.RowVersion),
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToPagedResultAsync(query.PageNumber, query.PageSize);

        return new PaymentQueryResult<PagedResultDto<PaymentResponseDto>>
        {
            Status = PaymentQueryStatus.Success,
            Data = result
        };
    }

    public async Task<PaymentQueryResult<PaymentResponseDto>> GetByIdAsync(
        Guid paymentId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var isAdminOrManager =
            currentUserRoles.Contains(AppRoles.Admin) ||
            currentUserRoles.Contains(AppRoles.Manager);

        var payment = await _context.Payments
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .Include(x => x.ReviewedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == paymentId);

        if (payment is null)
        {
            return new PaymentQueryResult<PaymentResponseDto>
            {
                Status = PaymentQueryStatus.PaymentNotFound
            };
        }

        if (!isAdminOrManager && payment.SalesRepId != currentUser.Id)
        {
            return new PaymentQueryResult<PaymentResponseDto>
            {
                Status = PaymentQueryStatus.Forbidden
            };
        }

        return new PaymentQueryResult<PaymentResponseDto>
        {
            Status = PaymentQueryStatus.Success,
            Data = MapPayment(payment)
        };
    }

    public async Task<PaymentQueryResult<IReadOnlyList<PaymentActionHistoryResponseDto>>> GetHistoryAsync(
        Guid paymentId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var isAdminOrManager =
            currentUserRoles.Contains(AppRoles.Admin) ||
            currentUserRoles.Contains(AppRoles.Manager);

        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == paymentId);

        if (payment is null)
        {
            return new PaymentQueryResult<IReadOnlyList<PaymentActionHistoryResponseDto>>
            {
                Status = PaymentQueryStatus.PaymentNotFound
            };
        }

        if (!isAdminOrManager && payment.SalesRepId != currentUser.Id)
        {
            return new PaymentQueryResult<IReadOnlyList<PaymentActionHistoryResponseDto>>
            {
                Status = PaymentQueryStatus.Forbidden
            };
        }

        var history = await _context.PaymentActionHistories
            .Where(x => x.PaymentId == paymentId)
            .OrderByDescending(x => x.ActionAtUtc)
            .Select(x => new PaymentActionHistoryResponseDto
            {
                Id = x.Id,
                PaymentId = x.PaymentId,
                ActionType = x.ActionType,
                PreviousStatus = x.PreviousStatus,
                NewStatus = x.NewStatus,
                PerformedByUserId = x.PerformedByUserId,
                PerformedByUserName = x.PerformedByUserFullName,
                BalanceBeforeAction = x.BalanceBeforeAction,
                BalanceAfterAction = x.BalanceAfterAction,
                Comment = x.Comment,
                ActionAtUtc = x.ActionAtUtc
            })
            .AsNoTracking()
            .ToListAsync();

        return new PaymentQueryResult<IReadOnlyList<PaymentActionHistoryResponseDto>>
        {
            Status = PaymentQueryStatus.Success,
            Data = history
        };
    }

    public async Task<PaymentQueryResult<PaymentReviewQueueResponseDto>> GetReviewQueueAsync(
        GetPaymentReviewQueueQueryDto query)
    {
        var now = DateTime.UtcNow;
        var submittedToUtc = NormalizeSubmittedToUtc(query.SubmittedToUtc);
        var staleCutoffUtc = now.AddHours(-query.StaleAfterHours);

        var pendingPaymentsQuery = _context.Payments
            .Where(x => x.Status == PaymentStatus.Pending)
            .AsNoTracking()
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            pendingPaymentsQuery = pendingPaymentsQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);

        if (query.CustomerId.HasValue)
            pendingPaymentsQuery = pendingPaymentsQuery.Where(x => x.CustomerId == query.CustomerId.Value);

        if (query.MinAmount.HasValue)
            pendingPaymentsQuery = pendingPaymentsQuery.Where(x => x.Amount >= query.MinAmount.Value);

        if (query.MaxAmount.HasValue)
            pendingPaymentsQuery = pendingPaymentsQuery.Where(x => x.Amount <= query.MaxAmount.Value);

        if (query.SubmittedFromUtc.HasValue)
            pendingPaymentsQuery = pendingPaymentsQuery.Where(x => x.CreatedAtUtc >= query.SubmittedFromUtc.Value);

        if (submittedToUtc.HasValue)
            pendingPaymentsQuery = pendingPaymentsQuery.Where(x => x.CreatedAtUtc < submittedToUtc.Value);

        if (query.StaleOnly)
            pendingPaymentsQuery = pendingPaymentsQuery.Where(x => x.CreatedAtUtc <= staleCutoffUtc);

        var summaryRows = await pendingPaymentsQuery
            .Select(x => new ReviewQueueSummaryRow
            {
                CustomerId = x.CustomerId,
                SalesRepId = x.SalesRepId,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                Reference = x.Reference,
                SubmittedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        var filteredCustomerIds = summaryRows
            .Select(x => x.CustomerId)
            .Distinct()
            .ToList();

        var currentBalancesByCustomerId = await BuildCurrentBalanceLookupAsync(filteredCustomerIds);
        var pendingCountsByCustomerId = await BuildPendingCountsByCustomerAsync(filteredCustomerIds);
        var duplicateReferenceCounts = await BuildDuplicateReferenceLookupAsync(filteredCustomerIds);

        var pagedQueueRows = await pendingPaymentsQuery
            .OrderBy(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Amount)
            .Select(x => new ReviewQueuePageRow
            {
                PaymentId = x.Id,
                PaymentNumber = x.PaymentNumber,
                VisitId = x.VisitId,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.Name,
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                Reference = x.Reference,
                SubmittedAtUtc = x.CreatedAtUtc
            })
            .ToPagedResultAsync(query.PageNumber, query.PageSize);

        var queueItems = pagedQueueRows.Items
            .Select(row => MapReviewQueueItem(
                row,
                now,
                query.StaleAfterHours,
                currentBalancesByCustomerId,
                pendingCountsByCustomerId,
                duplicateReferenceCounts))
            .ToList();

        var summary = BuildReviewQueueSummary(
            summaryRows,
            now,
            query.StaleAfterHours,
            currentBalancesByCustomerId,
            pendingCountsByCustomerId,
            duplicateReferenceCounts);

        return new PaymentQueryResult<PaymentReviewQueueResponseDto>
        {
            Status = PaymentQueryStatus.Success,
            Data = new PaymentReviewQueueResponseDto
            {
                GeneratedAtUtc = now,
                StaleAfterHours = query.StaleAfterHours,
                Summary = summary,
                Queue = new PagedResultDto<PaymentReviewQueueItemDto>
                {
                    Items = queueItems,
                    PageNumber = pagedQueueRows.PageNumber,
                    PageSize = pagedQueueRows.PageSize,
                    TotalCount = pagedQueueRows.TotalCount
                }
            }
        };
    }

    public async Task<PaymentQueryResult<PaymentOperationsReportResponseDto>> GetOperationsReportAsync(
        GetPaymentOperationsReportQueryDto query)
    {
        var now = DateTime.UtcNow;
        var range = NormalizeOperationsReportRange(query, now);

        var submissionsWindowQuery = _context.Payments
            .AsNoTracking()
            .Where(x => x.CreatedAtUtc >= range.FromUtc && x.CreatedAtUtc < range.ToUtc)
            .AsQueryable();

        var decisionsWindowQuery = _context.Payments
            .AsNoTracking()
            .Where(x =>
                x.ReviewedAtUtc.HasValue &&
                x.ReviewedAtUtc.Value >= range.FromUtc &&
                x.ReviewedAtUtc.Value < range.ToUtc &&
                (x.Status == PaymentStatus.Approved || x.Status == PaymentStatus.Rejected || x.Status == PaymentStatus.Voided))
            .AsQueryable();

        var currentPendingQuery = _context.Payments
            .AsNoTracking()
            .Where(x => x.Status == PaymentStatus.Pending)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
        {
            submissionsWindowQuery = submissionsWindowQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);
            decisionsWindowQuery = decisionsWindowQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);
            currentPendingQuery = currentPendingQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);
        }

        if (query.CustomerId.HasValue)
        {
            submissionsWindowQuery = submissionsWindowQuery.Where(x => x.CustomerId == query.CustomerId.Value);
            decisionsWindowQuery = decisionsWindowQuery.Where(x => x.CustomerId == query.CustomerId.Value);
            currentPendingQuery = currentPendingQuery.Where(x => x.CustomerId == query.CustomerId.Value);
        }

        var submissionRows = await submissionsWindowQuery
            .Select(x => new SubmissionWindowRow
            {
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod
            })
            .ToListAsync();

        var decisionRows = await decisionsWindowQuery
            .Select(x => new DecisionWindowRow
            {
                Status = x.Status,
                Amount = x.Amount,
                CreatedAtUtc = x.CreatedAtUtc,
                DecidedAtUtc = x.ReviewedAtUtc!.Value,
                ReviewerUserId = x.ReviewedByUserId,
                ReviewerName = x.ReviewedByUser != null ? x.ReviewedByUser.FullName : string.Empty,
                RejectionReason = x.RejectionReason
            })
            .ToListAsync();

        var backlogRows = await currentPendingQuery
            .Select(x => new PendingBacklogRow
            {
                CustomerId = x.CustomerId,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                Reference = x.Reference,
                SubmittedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        var backlogCustomerIds = backlogRows
            .Select(x => x.CustomerId)
            .Distinct()
            .ToList();

        var currentBalancesByCustomerId = await BuildCurrentBalanceLookupAsync(backlogCustomerIds);
        var pendingCountsByCustomerId = await BuildPendingCountsByCustomerAsync(backlogCustomerIds);
        var duplicateReferenceCounts = await BuildDuplicateReferenceLookupAsync(backlogCustomerIds);

        var backlogSnapshot = BuildOperationsBacklogSnapshot(
            backlogRows,
            now,
            query.StaleAfterHours,
            currentBalancesByCustomerId,
            pendingCountsByCustomerId,
            duplicateReferenceCounts);

        var throughputSummary = BuildOperationsThroughputSummary(submissionRows, decisionRows);

        var pendingAgingBuckets = BuildPendingAgingBuckets(backlogRows, now);

        var submissionMethodBreakdown = submissionRows
            .GroupBy(x => x.PaymentMethod)
            .OrderBy(g => g.Key)
            .Select(g => new PaymentMethodBreakdownDto
            {
                PaymentMethod = g.Key,
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount)
            })
            .ToList();

        var rejectionCategoryBreakdown = decisionRows
            .Where(x => x.Status == PaymentStatus.Rejected)
            .GroupBy(x => ParseRejectionCategory(x.RejectionReason))
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => new PaymentRejectionCategoryBreakdownDto
            {
                Category = g.Key,
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount)
            })
            .ToList();

        var reviewerPerformance = decisionRows
            .Where(x => x.ReviewerUserId.HasValue)
            .GroupBy(x => new { ReviewerUserId = x.ReviewerUserId!.Value, x.ReviewerName })
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key.ReviewerName)
            .Select(g => new PaymentReviewerPerformanceDto
            {
                ReviewerUserId = g.Key.ReviewerUserId,
                ReviewerName = string.IsNullOrWhiteSpace(g.Key.ReviewerName) ? "Unknown Reviewer" : g.Key.ReviewerName,
                ReviewedCount = g.Count(),
                ApprovedCount = g.Count(x => x.Status == PaymentStatus.Approved),
                RejectedCount = g.Count(x => x.Status == PaymentStatus.Rejected),
                ReviewedAmount = g.Sum(x => x.Amount),
                AverageDecisionTurnaroundHours = Math.Round(g.Average(x => (x.DecidedAtUtc - x.CreatedAtUtc).TotalHours), 2)
            })
            .ToList();

        return new PaymentQueryResult<PaymentOperationsReportResponseDto>
        {
            Status = PaymentQueryStatus.Success,
            Data = new PaymentOperationsReportResponseDto
            {
                GeneratedAtUtc = now,
                RangeFromUtc = range.FromUtc,
                RangeToUtc = range.ToUtc,
                StaleAfterHours = query.StaleAfterHours,
                BacklogSnapshot = backlogSnapshot,
                ThroughputSummary = throughputSummary,
                PendingAgingBuckets = pendingAgingBuckets,
                SubmissionMethodBreakdown = submissionMethodBreakdown,
                RejectionCategoryBreakdown = rejectionCategoryBreakdown,
                ReviewerPerformance = reviewerPerformance
            }
        };
    }

    private async Task<Dictionary<Guid, decimal>> BuildCurrentBalanceLookupAsync(
        IReadOnlyCollection<Guid> customerIds)
    {
        if (customerIds.Count == 0)
            return new Dictionary<Guid, decimal>();

        var balanceSnapshots = await _customerBalanceService.GetSnapshotsAsync(customerIds);

        return balanceSnapshots.ToDictionary(
            x => x.Key,
            x => x.Value.CurrentBalance);
    }

    private async Task<Dictionary<Guid, int>> BuildPendingCountsByCustomerAsync(
        IReadOnlyCollection<Guid> customerIds)
    {
        if (customerIds.Count == 0)
            return new Dictionary<Guid, int>();

        var counts = await _context.Payments
            .Where(x => customerIds.Contains(x.CustomerId) && x.Status == PaymentStatus.Pending)
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count()
            })
            .AsNoTracking()
            .ToListAsync();

        return counts.ToDictionary(x => x.CustomerId, x => x.Count);
    }

    private async Task<Dictionary<(Guid CustomerId, string Reference), int>> BuildDuplicateReferenceLookupAsync(
        IReadOnlyCollection<Guid> customerIds)
    {
        if (customerIds.Count == 0)
            return new Dictionary<(Guid CustomerId, string Reference), int>();

        var pendingReferences = await _context.Payments
            .AsNoTracking()
            .Where(x =>
                customerIds.Contains(x.CustomerId) &&
                x.Status == PaymentStatus.Pending &&
                x.Reference != null)
            .Select(x => new
            {
                x.CustomerId,
                x.Reference
            })
            .ToListAsync();

        return pendingReferences
            .Select(x => new
            {
                x.CustomerId,
                Reference = PaymentReferenceNormalizer.Normalize(x.Reference)
            })
            .Where(x => x.Reference is not null)
            .GroupBy(x => new { x.CustomerId, x.Reference })
            .ToDictionary(
                x => (x.Key.CustomerId, x.Key.Reference!),
                x => x.Count());
    }

    private static PaymentReviewQueueItemDto MapReviewQueueItem(
        ReviewQueuePageRow row,
        DateTime now,
        int staleAfterHours,
        IReadOnlyDictionary<Guid, decimal> currentBalancesByCustomerId,
        IReadOnlyDictionary<Guid, int> pendingCountsByCustomerId,
        IReadOnlyDictionary<(Guid CustomerId, string Reference), int> duplicateReferenceCounts)
    {
        var currentOutstandingBalance = currentBalancesByCustomerId.GetValueOrDefault(row.CustomerId, 0m);
        var pendingPaymentsForCustomerCount = pendingCountsByCustomerId.GetValueOrDefault(row.CustomerId, 0);
        var duplicatePendingReferenceCount = GetDuplicateReferenceCount(row.CustomerId, row.Reference, duplicateReferenceCounts);
        var pendingForHours = Math.Round((now - row.SubmittedAtUtc).TotalHours, 2);
        var isStale = pendingForHours >= staleAfterHours;
        var balanceCoverageRatio = currentOutstandingBalance > 0
            ? (decimal?)Math.Round(row.Amount / currentOutstandingBalance, 4)
            : null;

        var riskFlags = BuildRiskFlags(
            row.Amount,
            row.PaymentMethod,
            row.Reference,
            pendingForHours,
            staleAfterHours,
            currentOutstandingBalance,
            pendingPaymentsForCustomerCount,
            duplicatePendingReferenceCount);

        return new PaymentReviewQueueItemDto
        {
            PaymentId = row.PaymentId,
            PaymentNumber = row.PaymentNumber,
            VisitId = row.VisitId,
            CustomerId = row.CustomerId,
            CustomerName = row.CustomerName,
            SalesRepId = row.SalesRepId,
            SalesRepName = row.SalesRepName,
            Amount = row.Amount,
            PaymentMethod = row.PaymentMethod,
            Reference = row.Reference,
            SubmittedAtUtc = row.SubmittedAtUtc,
            PendingForHours = pendingForHours,
            IsStale = isStale,
            CurrentOutstandingBalance = currentOutstandingBalance,
            BalanceCoverageRatio = balanceCoverageRatio,
            PendingPaymentsForCustomerCount = pendingPaymentsForCustomerCount,
            DuplicatePendingReferenceCount = duplicatePendingReferenceCount,
            ReviewRiskFlags = riskFlags
        };
    }

    private static PaymentReviewQueueSummaryDto BuildReviewQueueSummary(
        IReadOnlyCollection<ReviewQueueSummaryRow> rows,
        DateTime now,
        int staleAfterHours,
        IReadOnlyDictionary<Guid, decimal> currentBalancesByCustomerId,
        IReadOnlyDictionary<Guid, int> pendingCountsByCustomerId,
        IReadOnlyDictionary<(Guid CustomerId, string Reference), int> duplicateReferenceCounts)
    {
        var oldestPending = rows.Count == 0
            ? null
            : rows.MinBy(x => x.SubmittedAtUtc);

        var attentionRequiredCount = 0;
        var approvalBlockedCount = 0;
        var missingReferenceForNonCashCount = 0;
        var duplicateReferencePendingCount = 0;
        var multiPendingCustomerPaymentCount = 0;

        foreach (var row in rows)
        {
            var pendingForHours = (now - row.SubmittedAtUtc).TotalHours;
            var currentOutstandingBalance = currentBalancesByCustomerId.GetValueOrDefault(row.CustomerId, 0m);
            var pendingPaymentsForCustomerCount = pendingCountsByCustomerId.GetValueOrDefault(row.CustomerId, 0);
            var duplicatePendingReferenceCount = GetDuplicateReferenceCount(row.CustomerId, row.Reference, duplicateReferenceCounts);

            var riskFlags = BuildRiskFlags(
                row.Amount,
                row.PaymentMethod,
                row.Reference,
                pendingForHours,
                staleAfterHours,
                currentOutstandingBalance,
                pendingPaymentsForCustomerCount,
                duplicatePendingReferenceCount);

            if (riskFlags.Count > 0)
                attentionRequiredCount++;

            if (riskFlags.Contains(PaymentReviewRiskFlag.ApprovalBlockedByBalance))
                approvalBlockedCount++;

            if (riskFlags.Contains(PaymentReviewRiskFlag.MissingReferenceForNonCash))
                missingReferenceForNonCashCount++;

            if (riskFlags.Contains(PaymentReviewRiskFlag.DuplicateReferenceInPendingQueue))
                duplicateReferencePendingCount++;

            if (riskFlags.Contains(PaymentReviewRiskFlag.MultiplePendingPaymentsForCustomer))
                multiPendingCustomerPaymentCount++;
        }

        return new PaymentReviewQueueSummaryDto
        {
            TotalPendingCount = rows.Count,
            TotalPendingAmount = rows.Sum(x => x.Amount),
            StalePendingCount = rows.Count(x => (now - x.SubmittedAtUtc).TotalHours >= staleAfterHours),
            ApprovalBlockedCount = approvalBlockedCount,
            AttentionRequiredCount = attentionRequiredCount,
            MissingReferenceForNonCashCount = missingReferenceForNonCashCount,
            DuplicateReferencePendingCount = duplicateReferencePendingCount,
            MultiPendingCustomerPaymentCount = multiPendingCustomerPaymentCount,
            OldestPendingSubmittedAtUtc = oldestPending?.SubmittedAtUtc,
            OldestPendingAgeInHours = oldestPending is null
                ? null
                : Math.Round((now - oldestPending.SubmittedAtUtc).TotalHours, 2)
        };
    }

    private static PaymentOperationsBacklogSnapshotDto BuildOperationsBacklogSnapshot(
        IReadOnlyCollection<PendingBacklogRow> rows,
        DateTime now,
        int staleAfterHours,
        IReadOnlyDictionary<Guid, decimal> currentBalancesByCustomerId,
        IReadOnlyDictionary<Guid, int> pendingCountsByCustomerId,
        IReadOnlyDictionary<(Guid CustomerId, string Reference), int> duplicateReferenceCounts)
    {
        if (rows.Count == 0)
        {
            return new PaymentOperationsBacklogSnapshotDto();
        }

        var stalePendingCount = 0;
        var stalePendingAmount = 0m;
        var approvalBlockedCount = 0;
        var attentionRequiredCount = 0;
        var pendingNonCashWithoutReferenceCount = 0;
        var pendingDuplicateReferenceCount = 0;

        foreach (var row in rows)
        {
            var pendingForHours = (now - row.SubmittedAtUtc).TotalHours;
            var currentOutstandingBalance = currentBalancesByCustomerId.GetValueOrDefault(row.CustomerId, 0m);
            var pendingPaymentsForCustomerCount = pendingCountsByCustomerId.GetValueOrDefault(row.CustomerId, 0);
            var duplicatePendingReferenceCount = GetDuplicateReferenceCount(row.CustomerId, row.Reference, duplicateReferenceCounts);

            var riskFlags = BuildRiskFlags(
                row.Amount,
                row.PaymentMethod,
                row.Reference,
                pendingForHours,
                staleAfterHours,
                currentOutstandingBalance,
                pendingPaymentsForCustomerCount,
                duplicatePendingReferenceCount);

            if (pendingForHours >= staleAfterHours)
            {
                stalePendingCount++;
                stalePendingAmount += row.Amount;
            }

            if (riskFlags.Count > 0)
                attentionRequiredCount++;

            if (riskFlags.Contains(PaymentReviewRiskFlag.ApprovalBlockedByBalance))
                approvalBlockedCount++;

            if (riskFlags.Contains(PaymentReviewRiskFlag.MissingReferenceForNonCash))
                pendingNonCashWithoutReferenceCount++;

            if (riskFlags.Contains(PaymentReviewRiskFlag.DuplicateReferenceInPendingQueue))
                pendingDuplicateReferenceCount++;
        }

        var averagePendingAgeInHours = Math.Round(rows.Average(x => (now - x.SubmittedAtUtc).TotalHours), 2);
        var oldestPendingAgeInHours = Math.Round(rows.Max(x => (now - x.SubmittedAtUtc).TotalHours), 2);

        return new PaymentOperationsBacklogSnapshotDto
        {
            PendingCount = rows.Count,
            PendingAmount = rows.Sum(x => x.Amount),
            CustomersWithPendingPaymentsCount = rows.Select(x => x.CustomerId).Distinct().Count(),
            StalePendingCount = stalePendingCount,
            StalePendingAmount = stalePendingAmount,
            ApprovalBlockedCount = approvalBlockedCount,
            AttentionRequiredCount = attentionRequiredCount,
            PendingNonCashWithoutReferenceCount = pendingNonCashWithoutReferenceCount,
            PendingDuplicateReferenceCount = pendingDuplicateReferenceCount,
            AveragePendingAgeInHours = averagePendingAgeInHours,
            OldestPendingAgeInHours = oldestPendingAgeInHours
        };
    }

    private static PaymentOperationsThroughputSummaryDto BuildOperationsThroughputSummary(
        IReadOnlyCollection<SubmissionWindowRow> submissionRows,
        IReadOnlyCollection<DecisionWindowRow> decisionRows)
    {
        var approvedRows = decisionRows.Where(x => x.Status == PaymentStatus.Approved).ToList();
        var rejectedRows = decisionRows.Where(x => x.Status == PaymentStatus.Rejected).ToList();
        var reviewedCount = decisionRows.Count;

        return new PaymentOperationsThroughputSummaryDto
        {
            SubmittedCount = submissionRows.Count,
            SubmittedAmount = submissionRows.Sum(x => x.Amount),
            ReviewedCount = reviewedCount,
            ReviewedAmount = decisionRows.Sum(x => x.Amount),
            ApprovedCount = approvedRows.Count,
            ApprovedAmount = approvedRows.Sum(x => x.Amount),
            RejectedCount = rejectedRows.Count,
            RejectedAmount = rejectedRows.Sum(x => x.Amount),
            ApprovalRatePercent = reviewedCount == 0
                ? null
                : Math.Round((double)approvedRows.Count / reviewedCount * 100d, 2),
            RejectionRatePercent = reviewedCount == 0
                ? null
                : Math.Round((double)rejectedRows.Count / reviewedCount * 100d, 2),
            AverageApprovalTurnaroundHours = approvedRows.Count == 0
                ? null
                : Math.Round(approvedRows.Average(x => (x.DecidedAtUtc - x.CreatedAtUtc).TotalHours), 2),
            AverageRejectionTurnaroundHours = rejectedRows.Count == 0
                ? null
                : Math.Round(rejectedRows.Average(x => (x.DecidedAtUtc - x.CreatedAtUtc).TotalHours), 2)
        };
    }

    private static List<PaymentPendingAgingBucketDto> BuildPendingAgingBuckets(
        IReadOnlyCollection<PendingBacklogRow> rows,
        DateTime now)
    {
        var buckets = new List<PaymentPendingAgingBucketDto>
        {
            BuildAgingBucket(rows, now, "0-24h", 0, 24),
            BuildAgingBucket(rows, now, "24-48h", 24, 48),
            BuildAgingBucket(rows, now, "48-72h", 48, 72),
            BuildAgingBucket(rows, now, "72h+", 72, null)
        };

        return buckets;
    }

    private static PaymentPendingAgingBucketDto BuildAgingBucket(
        IReadOnlyCollection<PendingBacklogRow> rows,
        DateTime now,
        string label,
        double minHoursInclusive,
        double? maxHoursExclusive)
    {
        var matchingRows = rows
            .Where(x =>
            {
                var age = (now - x.SubmittedAtUtc).TotalHours;
                return age >= minHoursInclusive &&
                       (!maxHoursExclusive.HasValue || age < maxHoursExclusive.Value);
            })
            .ToList();

        return new PaymentPendingAgingBucketDto
        {
            Label = label,
            Count = matchingRows.Count,
            Amount = matchingRows.Sum(x => x.Amount)
        };
    }

    private static List<PaymentReviewRiskFlag> BuildRiskFlags(
        decimal amount,
        PaymentMethod paymentMethod,
        string? reference,
        double pendingForHours,
        int staleAfterHours,
        decimal currentOutstandingBalance,
        int pendingPaymentsForCustomerCount,
        int duplicatePendingReferenceCount)
    {
        var riskFlags = new List<PaymentReviewRiskFlag>();

        if (pendingForHours >= staleAfterHours)
            riskFlags.Add(PaymentReviewRiskFlag.Stale);

        if (currentOutstandingBalance <= 0 || amount > currentOutstandingBalance)
            riskFlags.Add(PaymentReviewRiskFlag.ApprovalBlockedByBalance);

        if (currentOutstandingBalance > 0 && amount / currentOutstandingBalance >= HighBalanceImpactRatioThreshold)
            riskFlags.Add(PaymentReviewRiskFlag.HighBalanceImpact);

        if (paymentMethod != PaymentMethod.Cash && string.IsNullOrWhiteSpace(reference))
            riskFlags.Add(PaymentReviewRiskFlag.MissingReferenceForNonCash);

        if (pendingPaymentsForCustomerCount > 1)
            riskFlags.Add(PaymentReviewRiskFlag.MultiplePendingPaymentsForCustomer);

        if (duplicatePendingReferenceCount > 1)
            riskFlags.Add(PaymentReviewRiskFlag.DuplicateReferenceInPendingQueue);

        return riskFlags;
    }

    private static int GetDuplicateReferenceCount(
        Guid customerId,
        string? reference,
        IReadOnlyDictionary<(Guid CustomerId, string Reference), int> duplicateReferenceCounts)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return 0;

        var normalizedReference = PaymentReferenceNormalizer.Normalize(reference);
        if (normalizedReference is null)
            return 0;

        return duplicateReferenceCounts.GetValueOrDefault((customerId, normalizedReference), 0);
    }

    private static (DateTime FromUtc, DateTime ToUtc) NormalizeOperationsReportRange(
        GetPaymentOperationsReportQueryDto query,
        DateTime now)
    {
        var toUtc = query.DateToUtc ?? now;
        if (toUtc.TimeOfDay == TimeSpan.Zero)
            toUtc = toUtc.Date.AddDays(1);

        var fromUtc = query.DateFromUtc ?? toUtc.AddDays(-7);

        return (fromUtc, toUtc);
    }

    private static DateTime? NormalizeCreatedToUtc(DateTime? createdToUtc)
    {
        if (!createdToUtc.HasValue)
            return null;

        return createdToUtc.Value.TimeOfDay == TimeSpan.Zero
            ? createdToUtc.Value.Date.AddDays(1)
            : createdToUtc.Value;
    }

    private static DateTime? NormalizeSubmittedToUtc(DateTime? submittedToUtc)
    {
        if (!submittedToUtc.HasValue)
            return null;

        return submittedToUtc.Value.TimeOfDay == TimeSpan.Zero
            ? submittedToUtc.Value.Date.AddDays(1)
            : submittedToUtc.Value;
    }

    private static PaymentRejectionCategory ParseRejectionCategory(string? rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            return PaymentRejectionCategory.Other;

        const string prefix = "Category:";
        if (!rejectionReason.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return PaymentRejectionCategory.Other;

        var rawValue = rejectionReason[prefix.Length..].Trim();
        var categoryToken = rawValue.Split('|', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];

        return Enum.TryParse<PaymentRejectionCategory>(categoryToken, true, out var category)
            ? category
            : PaymentRejectionCategory.Other;
    }

    private static PaymentResponseDto MapPayment(Payment payment)
    {
        return new PaymentResponseDto
        {
            Id = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            VisitId = payment.VisitId,
            CustomerId = payment.CustomerId,
            CustomerName = payment.Customer.Name,
            SalesRepId = payment.SalesRepId,
            SalesRepName = payment.SalesRep.FullName,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            Reference = payment.Reference,
            Notes = payment.Notes,
            ReviewedByUserId = payment.ReviewedByUserId,
            ReviewedByUserName = payment.ReviewedByUser?.FullName,
            ReviewedAtUtc = payment.ReviewedAtUtc,
            RejectionReason = payment.RejectionReason,
            RowVersion = RowVersionTokenHelper.Encode(payment.RowVersion),
            CreatedAtUtc = payment.CreatedAtUtc,
            UpdatedAtUtc = payment.UpdatedAtUtc
        };
    }

    private sealed class ReviewQueueSummaryRow
    {
        public Guid CustomerId { get; init; }
        public Guid SalesRepId { get; init; }
        public decimal Amount { get; init; }
        public PaymentMethod PaymentMethod { get; init; }
        public string? Reference { get; init; }
        public DateTime SubmittedAtUtc { get; init; }
    }

    private sealed class ReviewQueuePageRow
    {
        public Guid PaymentId { get; init; }
        public string PaymentNumber { get; init; } = string.Empty;
        public Guid VisitId { get; init; }
        public Guid CustomerId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public Guid SalesRepId { get; init; }
        public string SalesRepName { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public PaymentMethod PaymentMethod { get; init; }
        public string? Reference { get; init; }
        public DateTime SubmittedAtUtc { get; init; }
    }

    private sealed class PendingBacklogRow
    {
        public Guid CustomerId { get; init; }
        public decimal Amount { get; init; }
        public PaymentMethod PaymentMethod { get; init; }
        public string? Reference { get; init; }
        public DateTime SubmittedAtUtc { get; init; }
    }

    private sealed class SubmissionWindowRow
    {
        public decimal Amount { get; init; }
        public PaymentMethod PaymentMethod { get; init; }
    }

    private sealed class DecisionWindowRow
    {
        public PaymentStatus Status { get; init; }
        public decimal Amount { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public DateTime DecidedAtUtc { get; init; }
        public Guid? ReviewerUserId { get; init; }
        public string ReviewerName { get; init; } = string.Empty;
        public string? RejectionReason { get; init; }
    }
}