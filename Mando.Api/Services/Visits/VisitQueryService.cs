using Microsoft.EntityFrameworkCore;
using Mando.Api.Common;
using Mando.Api.Data;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Visits;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Visits;
using Mando.Api.Models.Visits;

namespace Mando.Api.Services.Visits;

public class VisitQueryService : IVisitQueryService
{
    private readonly AppDbContext _context;

    public VisitQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VisitQueryResult<PagedResultDto<VisitResponseDto>>> GetAllAsync(
        GetVisitsQueryDto query,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var visitsQuery = _context.Visits
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .AsQueryable();

        if (!IsAdminOrManager(currentUserRoles))
        {
            visitsQuery = visitsQuery.Where(x => x.SalesRepId == currentUser.Id);
        }

        if (query.CustomerId.HasValue)
            visitsQuery = visitsQuery.Where(x => x.CustomerId == query.CustomerId.Value);

        if (query.Status.HasValue)
            visitsQuery = visitsQuery.Where(x => x.Status == query.Status.Value);

        if (query.DateFromUtc.HasValue)
            visitsQuery = visitsQuery.Where(x => x.CheckInAtUtc >= query.DateFromUtc.Value);

        var normalizedDateToUtc = NormalizeCheckInToUtc(query.DateToUtc);

        if (normalizedDateToUtc.HasValue)
            visitsQuery = visitsQuery.Where(x => x.CheckInAtUtc < normalizedDateToUtc.Value);

        var result = await visitsQuery
            .OrderByDescending(x => x.CheckInAtUtc)
            .AsNoTracking()
            .Select(x => MapVisit(x, x.Customer.Name, x.SalesRep.FullName))
            .ToPagedResultAsync(query.PageNumber, query.PageSize);

        return new VisitQueryResult<PagedResultDto<VisitResponseDto>>
        {
            Status = VisitQueryStatus.Success,
            Data = result
        };
    }

    public async Task<VisitQueryResult<VisitResponseDto>> GetByIdAsync(
        Guid visitId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var visit = await _context.Visits
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == visitId);

        if (visit is null)
            return new VisitQueryResult<VisitResponseDto> { Status = VisitQueryStatus.VisitNotFound };

        if (!IsAdminOrManager(currentUserRoles) && visit.SalesRepId != currentUser.Id)
            return new VisitQueryResult<VisitResponseDto> { Status = VisitQueryStatus.Forbidden };

        return new VisitQueryResult<VisitResponseDto>
        {
            Status = VisitQueryStatus.Success,
            Data = MapVisit(visit, visit.Customer.Name, visit.SalesRep.FullName)
        };
    }
    public async Task<VisitQueryResult<IReadOnlyList<VisitActionHistoryResponseDto>>> GetHistoryAsync(
        Guid visitId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var visit = await _context.Visits
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == visitId);

        if (visit is null)
            return new VisitQueryResult<IReadOnlyList<VisitActionHistoryResponseDto>> { Status = VisitQueryStatus.VisitNotFound };

        if (!IsAdminOrManager(currentUserRoles) && visit.SalesRepId != currentUser.Id)
            return new VisitQueryResult<IReadOnlyList<VisitActionHistoryResponseDto>> { Status = VisitQueryStatus.Forbidden };

        var history = await _context.VisitActionHistories
            .Where(x => x.VisitId == visitId)
            .OrderByDescending(x => x.ActionAtUtc)
            .Select(x => new VisitActionHistoryResponseDto
            {
                Id = x.Id,
                VisitId = x.VisitId,
                ActionType = x.ActionType,
                PreviousStatus = x.PreviousStatus,
                NewStatus = x.NewStatus,
                PreviousOutcome = x.PreviousOutcome,
                NewOutcome = x.NewOutcome,
                PerformedByUserId = x.PerformedByUserId,
                PerformedByUserName = x.PerformedByUserFullName,
                Comment = x.Comment,
                ActionAtUtc = x.ActionAtUtc
            })
            .AsNoTracking()
            .ToListAsync();

        return new VisitQueryResult<IReadOnlyList<VisitActionHistoryResponseDto>>
        {
            Status = VisitQueryStatus.Success,
            Data = history
        };
    }
    public async Task<VisitQueryResult<VisitTimelineResponseDto>> GetTimelineAsync(
        Guid visitId,
        string baseUrl,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var visit = await _context.Visits
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == visitId);

        if (visit is null)
            return new VisitQueryResult<VisitTimelineResponseDto> { Status = VisitQueryStatus.VisitNotFound };

        if (!IsAdminOrManager(currentUserRoles) && visit.SalesRepId != currentUser.Id)
            return new VisitQueryResult<VisitTimelineResponseDto> { Status = VisitQueryStatus.Forbidden };

        var images = await _context.VisitImages
            .Include(x => x.UploadedByUser)
            .Where(x => x.VisitId == visitId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new VisitTimelineImageDto
            {
                Id = x.Id,
                OriginalFileName = x.OriginalFileName,
                ImageUrl = BuildImageUrl(baseUrl, x.Id),
                ContentType = x.ContentType,
                FileSizeInBytes = x.FileSizeInBytes,
                UploadedByUserId = x.UploadedByUserId,
                UploadedByUserName = x.UploadedByUser.FullName,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .AsNoTracking()
            .ToListAsync();

        var orders = await _context.Orders
            .Where(x => x.VisitId == visitId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new VisitTimelineOrderDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                TotalAmount = x.TotalAmount,
                PaymentType = x.PaymentType,
                Notes = x.Notes,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .AsNoTracking()
            .ToListAsync();

        var payments = await _context.Payments
            .Where(x => x.VisitId == visitId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new VisitTimelinePaymentDto
            {
                Id = x.Id,
                PaymentNumber = x.PaymentNumber,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                Status = x.Status,
                Reference = x.Reference,
                Notes = x.Notes,
                RejectionReason = x.RejectionReason,
                CreatedAtUtc = x.CreatedAtUtc,
                ReviewedAtUtc = x.ReviewedAtUtc
            })
            .AsNoTracking()
            .ToListAsync();

        var response = new VisitTimelineResponseDto
        {
            VisitId = visit.Id,
            CustomerId = visit.CustomerId,
            CustomerName = visit.Customer.Name,
            SalesRepId = visit.SalesRepId,
            SalesRepName = visit.SalesRep.FullName,
            CheckInAtUtc = visit.CheckInAtUtc,
            CheckInLatitude = visit.CheckInLatitude,
            CheckInLongitude = visit.CheckInLongitude,
            CheckInAccuracyInMeters = visit.CheckInAccuracyInMeters,
            CheckOutAtUtc = visit.CheckOutAtUtc,
            CheckOutLatitude = visit.CheckOutLatitude,
            CheckOutLongitude = visit.CheckOutLongitude,
            CheckOutAccuracyInMeters = visit.CheckOutAccuracyInMeters,
            DistanceFromCustomerInMeters = visit.DistanceFromCustomerInMeters,
            Status = visit.Status,
            Outcome = visit.Outcome,
            Notes = visit.Notes,
            Images = images,
            Orders = orders,
            Payments = payments
        };

        return new VisitQueryResult<VisitTimelineResponseDto>
        {
            Status = VisitQueryStatus.Success,
            Data = response
        };
    }

    public async Task<VisitQueryResult<VisitOperationsReportResponseDto>> GetOperationsReportAsync(
        GetVisitOperationsReportQueryDto query)
    {
        var now = DateTime.UtcNow;
        var (fromUtc, toUtc) = NormalizeOperationsReportRange(query, now);

        var startedWindowQuery = _context.Visits
            .AsNoTracking()
            .Where(x => x.CheckInAtUtc >= fromUtc && x.CheckInAtUtc < toUtc)
            .AsQueryable();

        var currentInProgressQuery = _context.Visits
            .AsNoTracking()
            .Where(x => x.Status == VisitStatus.InProgress)
            .AsQueryable();

        var closedWindowQuery = _context.Visits
            .AsNoTracking()
            .Where(x =>
                x.CheckOutAtUtc.HasValue &&
                x.CheckOutAtUtc.Value >= fromUtc &&
                x.CheckOutAtUtc.Value < toUtc &&
                (x.Status == VisitStatus.Completed || x.Status == VisitStatus.Cancelled))
            .AsQueryable();

        if (query.SalesRepId.HasValue)
        {
            startedWindowQuery = startedWindowQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);
            currentInProgressQuery = currentInProgressQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);
            closedWindowQuery = closedWindowQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);
        }

        if (query.CustomerId.HasValue)
        {
            startedWindowQuery = startedWindowQuery.Where(x => x.CustomerId == query.CustomerId.Value);
            currentInProgressQuery = currentInProgressQuery.Where(x => x.CustomerId == query.CustomerId.Value);
            closedWindowQuery = closedWindowQuery.Where(x => x.CustomerId == query.CustomerId.Value);
        }

        var startedRows = await startedWindowQuery
            .Select(x => new StartedVisitRow
            {
                VisitId = x.Id,
                CustomerId = x.CustomerId,
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                CheckInAtUtc = x.CheckInAtUtc,
                Status = x.Status,
                Outcome = x.Outcome,
                CheckOutAtUtc = x.CheckOutAtUtc
            })
            .ToListAsync();

        var activeRows = await currentInProgressQuery
            .Select(x => new ActiveVisitRow
            {
                VisitId = x.Id,
                CustomerId = x.CustomerId,
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                CheckInAtUtc = x.CheckInAtUtc
            })
            .ToListAsync();

        var closedRows = await closedWindowQuery
            .Select(x => new ClosedVisitRow
            {
                VisitId = x.Id,
                CustomerId = x.CustomerId,
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                CheckInAtUtc = x.CheckInAtUtc,
                CheckOutAtUtc = x.CheckOutAtUtc!.Value,
                Status = x.Status,
                Outcome = x.Outcome
            })
            .ToListAsync();

        var relevantVisitIds = startedRows.Select(x => x.VisitId)
            .Concat(activeRows.Select(x => x.VisitId))
            .Concat(closedRows.Select(x => x.VisitId))
            .Distinct()
            .ToList();

        var visitIdsWithOrders = relevantVisitIds.Count == 0
            ? new HashSet<Guid>()
            : (await _context.Orders
                .Where(x => relevantVisitIds.Contains(x.VisitId))
                .Select(x => x.VisitId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

        var visitIdsWithPayments = relevantVisitIds.Count == 0
            ? new HashSet<Guid>()
            : (await _context.Payments
                .Where(x => relevantVisitIds.Contains(x.VisitId))
                .Select(x => x.VisitId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

        var activeSnapshot = BuildActiveSnapshot(activeRows, now, query.StaleAfterHours);
        var throughputSummary = BuildThroughputSummary(startedRows, closedRows, visitIdsWithOrders, visitIdsWithPayments);
        var outcomeBreakdown = BuildOutcomeBreakdown(closedRows);
        var salesRepPerformance = BuildSalesRepPerformance(startedRows, closedRows, visitIdsWithOrders, visitIdsWithPayments);
        var activeVisitAgingBuckets = BuildActiveVisitAgingBuckets(activeRows, now);

        return new VisitQueryResult<VisitOperationsReportResponseDto>
        {
            Status = VisitQueryStatus.Success,
            Data = new VisitOperationsReportResponseDto
            {
                GeneratedAtUtc = now,
                RangeFromUtc = fromUtc,
                RangeToUtc = toUtc,
                StaleAfterHours = query.StaleAfterHours,
                ActiveSnapshot = activeSnapshot,
                ThroughputSummary = throughputSummary,
                OutcomeBreakdown = outcomeBreakdown,
                SalesRepPerformance = salesRepPerformance,
                ActiveVisitAgingBuckets = activeVisitAgingBuckets
            }
        };
    }

    private static VisitOperationsActiveSnapshotDto BuildActiveSnapshot(
        IReadOnlyCollection<ActiveVisitRow> rows,
        DateTime now,
        int staleAfterHours)
    {
        if (rows.Count == 0)
            return new VisitOperationsActiveSnapshotDto();

        var staleRows = rows
            .Where(x => (now - x.CheckInAtUtc).TotalHours >= staleAfterHours)
            .ToList();

        return new VisitOperationsActiveSnapshotDto
        {
            InProgressCount = rows.Count,
            StaleInProgressCount = staleRows.Count,
            CustomersWithInProgressVisitsCount = rows.Select(x => x.CustomerId).Distinct().Count(),
            SalesRepsWithInProgressVisitsCount = rows.Select(x => x.SalesRepId).Distinct().Count(),
            AverageInProgressAgeInHours = Math.Round(rows.Average(x => (now - x.CheckInAtUtc).TotalHours), 2),
            OldestInProgressAgeInHours = Math.Round(rows.Max(x => (now - x.CheckInAtUtc).TotalHours), 2)
        };
    }

    private static VisitOperationsThroughputSummaryDto BuildThroughputSummary(
        IReadOnlyCollection<StartedVisitRow> startedRows,
        IReadOnlyCollection<ClosedVisitRow> closedRows,
        HashSet<Guid> visitIdsWithOrders,
        HashSet<Guid> visitIdsWithPayments)
    {
        var completedRows = closedRows.Where(x => x.Status == VisitStatus.Completed).ToList();
        var cancelledRows = closedRows.Where(x => x.Status == VisitStatus.Cancelled).ToList();

        return new VisitOperationsThroughputSummaryDto
        {
            StartedCount = startedRows.Count,
            CompletedCount = completedRows.Count,
            CancelledCount = cancelledRows.Count,
            CompletionRatePercent = startedRows.Count == 0
                ? null
                : Math.Round((double)completedRows.Count / startedRows.Count * 100d, 2),
            CancellationRatePercent = startedRows.Count == 0
                ? null
                : Math.Round((double)cancelledRows.Count / startedRows.Count * 100d, 2),
            AverageCompletedVisitDurationHours = completedRows.Count == 0
                ? null
                : Math.Round(completedRows.Average(x => (x.CheckOutAtUtc - x.CheckInAtUtc).TotalHours), 2),
            AverageCancelledVisitDurationHours = cancelledRows.Count == 0
                ? null
                : Math.Round(cancelledRows.Average(x => (x.CheckOutAtUtc - x.CheckInAtUtc).TotalHours), 2),
            VisitsWithOrdersCount = startedRows.Count(x => visitIdsWithOrders.Contains(x.VisitId)),
            VisitsWithPaymentsCount = startedRows.Count(x => visitIdsWithPayments.Contains(x.VisitId))
        };
    }

    private static List<VisitOutcomeBreakdownDto> BuildOutcomeBreakdown(
        IReadOnlyCollection<ClosedVisitRow> closedRows)
    {
        return closedRows
            .GroupBy(x => x.Outcome)
            .OrderBy(g => g.Key)
            .Select(g => new VisitOutcomeBreakdownDto
            {
                Outcome = g.Key,
                Count = g.Count()
            })
            .ToList();
    }

    private static List<VisitSalesRepPerformanceDto> BuildSalesRepPerformance(
        IReadOnlyCollection<StartedVisitRow> startedRows,
        IReadOnlyCollection<ClosedVisitRow> closedRows,
        HashSet<Guid> visitIdsWithOrders,
        HashSet<Guid> visitIdsWithPayments)
    {
        var salesRepIds = startedRows.Select(x => x.SalesRepId)
            .Concat(closedRows.Select(x => x.SalesRepId))
            .Distinct()
            .ToList();

        var result = new List<VisitSalesRepPerformanceDto>();

        foreach (var salesRepId in salesRepIds)
        {
            var repStarted = startedRows.Where(x => x.SalesRepId == salesRepId).ToList();
            var repClosed = closedRows.Where(x => x.SalesRepId == salesRepId).ToList();
            var repCompleted = repClosed.Where(x => x.Status == VisitStatus.Completed).ToList();
            var repCancelled = repClosed.Where(x => x.Status == VisitStatus.Cancelled).ToList();

            var salesRepName =
                repStarted.Select(x => x.SalesRepName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ??
                repClosed.Select(x => x.SalesRepName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ??
                "Unknown Sales Rep";

            result.Add(new VisitSalesRepPerformanceDto
            {
                SalesRepId = salesRepId,
                SalesRepName = salesRepName,
                StartedCount = repStarted.Count,
                CompletedCount = repCompleted.Count,
                CancelledCount = repCancelled.Count,
                SuccessfulVisitsCount = repCompleted.Count(x => x.Outcome == VisitOutcome.Successful),
                VisitsWithOrdersCount = repStarted.Count(x => visitIdsWithOrders.Contains(x.VisitId)),
                VisitsWithPaymentsCount = repStarted.Count(x => visitIdsWithPayments.Contains(x.VisitId)),
                AverageCompletedVisitDurationHours = repCompleted.Count == 0
                    ? null
                    : Math.Round(repCompleted.Average(x => (x.CheckOutAtUtc - x.CheckInAtUtc).TotalHours), 2)
            });
        }

        return result
            .OrderByDescending(x => x.StartedCount)
            .ThenBy(x => x.SalesRepName)
            .ToList();
    }

    private static List<VisitActiveAgingBucketDto> BuildActiveVisitAgingBuckets(
        IReadOnlyCollection<ActiveVisitRow> rows,
        DateTime now)
    {
        return
        [
            BuildAgingBucket(rows, now, "0-2h", 0, 2),
            BuildAgingBucket(rows, now, "2-4h", 2, 4),
            BuildAgingBucket(rows, now, "4-8h", 4, 8),
            BuildAgingBucket(rows, now, "8h+", 8, null)
        ];
    }

    private static VisitActiveAgingBucketDto BuildAgingBucket(
        IReadOnlyCollection<ActiveVisitRow> rows,
        DateTime now,
        string label,
        double minHoursInclusive,
        double? maxHoursExclusive)
    {
        var count = rows.Count(x =>
        {
            var age = (now - x.CheckInAtUtc).TotalHours;
            return age >= minHoursInclusive &&
                   (!maxHoursExclusive.HasValue || age < maxHoursExclusive.Value);
        });

        return new VisitActiveAgingBucketDto
        {
            Label = label,
            Count = count
        };
    }

    private static (DateTime FromUtc, DateTime ToUtc) NormalizeOperationsReportRange(
        GetVisitOperationsReportQueryDto query,
        DateTime now)
    {
        var toUtc = query.DateToUtc ?? now;
        if (toUtc.TimeOfDay == TimeSpan.Zero)
            toUtc = toUtc.Date.AddDays(1);

        var fromUtc = query.DateFromUtc ?? toUtc.AddDays(-7);

        return (fromUtc, toUtc);
    }


    private static DateTime? NormalizeCheckInToUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.TimeOfDay == TimeSpan.Zero
            ? value.Value.Date.AddDays(1)
            : value.Value;
    }

    private static bool IsAdminOrManager(IEnumerable<string> currentUserRoles)
    {
        return currentUserRoles.Contains(AppRoles.Admin) ||
               currentUserRoles.Contains(AppRoles.Manager);
    }

    private static string BuildImageUrl(string baseUrl, Guid imageId)
    {
        return $"{baseUrl.TrimEnd('/')}/api/visits/images/{imageId}/content";
    }

    private static VisitResponseDto MapVisit(Visit visit, string customerName, string salesRepName)
    {
        return new VisitResponseDto
        {
            Id = visit.Id,
            CustomerId = visit.CustomerId,
            CustomerName = customerName,
            SalesRepId = visit.SalesRepId,
            SalesRepName = salesRepName,
            CheckInAtUtc = visit.CheckInAtUtc,
            CheckInLatitude = visit.CheckInLatitude,
            CheckInLongitude = visit.CheckInLongitude,
            CheckInAccuracyInMeters = visit.CheckInAccuracyInMeters,
            CheckOutAtUtc = visit.CheckOutAtUtc,
            CheckOutLatitude = visit.CheckOutLatitude,
            CheckOutLongitude = visit.CheckOutLongitude,
            CheckOutAccuracyInMeters = visit.CheckOutAccuracyInMeters,
            DistanceFromCustomerInMeters = visit.DistanceFromCustomerInMeters,
            Status = visit.Status,
            Outcome = visit.Outcome,
            Notes = visit.Notes,
            RowVersion = RowVersionTokenHelper.Encode(visit.RowVersion),
            CreatedAtUtc = visit.CreatedAtUtc,
            UpdatedAtUtc = visit.UpdatedAtUtc
        };
    }

    private sealed class StartedVisitRow
    {
        public Guid VisitId { get; init; }
        public Guid CustomerId { get; init; }
        public Guid SalesRepId { get; init; }
        public string SalesRepName { get; init; } = string.Empty;
        public DateTime CheckInAtUtc { get; init; }
        public VisitStatus Status { get; init; }
        public VisitOutcome Outcome { get; init; }
        public DateTime? CheckOutAtUtc { get; init; }
    }

    private sealed class ActiveVisitRow
    {
        public Guid VisitId { get; init; }
        public Guid CustomerId { get; init; }
        public Guid SalesRepId { get; init; }
        public string SalesRepName { get; init; } = string.Empty;
        public DateTime CheckInAtUtc { get; init; }
    }

    private sealed class ClosedVisitRow
    {
        public Guid VisitId { get; init; }
        public Guid CustomerId { get; init; }
        public Guid SalesRepId { get; init; }
        public string SalesRepName { get; init; } = string.Empty;
        public DateTime CheckInAtUtc { get; init; }
        public DateTime CheckOutAtUtc { get; init; }
        public VisitStatus Status { get; init; }
        public VisitOutcome Outcome { get; init; }
    }
}
