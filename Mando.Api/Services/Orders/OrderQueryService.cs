using Microsoft.EntityFrameworkCore;
using Mando.Api.Common;
using Mando.Api.Data;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Orders;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Orders;
using Mando.Api.Models.Orders;

namespace Mando.Api.Services.Orders;

public class OrderQueryService : IOrderQueryService
{
    private readonly AppDbContext _context;

    public OrderQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OrderQueryResult<PagedResultDto<OrderResponseDto>>> GetAllAsync(
        GetOrdersQueryDto query,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var isAdminOrManager =
            currentUserRoles.Contains(AppRoles.Admin) ||
            currentUserRoles.Contains(AppRoles.Manager);

        var ordersQuery = _context.Orders.AsQueryable();

        if (!isAdminOrManager)
        {
            ordersQuery = ordersQuery.Where(x => x.SalesRepId == currentUser.Id);
        }

        if (query.CustomerId.HasValue)
            ordersQuery = ordersQuery.Where(x => x.CustomerId == query.CustomerId.Value);

        if (query.SalesRepId.HasValue)
            ordersQuery = ordersQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);

        if (query.VisitId.HasValue)
            ordersQuery = ordersQuery.Where(x => x.VisitId == query.VisitId.Value);

        if (query.PaymentType.HasValue)
            ordersQuery = ordersQuery.Where(x => x.PaymentType == query.PaymentType.Value);

        if (query.Status.HasValue)
            ordersQuery = ordersQuery.Where(x => x.Status == query.Status.Value);

        if (!string.IsNullOrWhiteSpace(query.OrderNumber))
        {
            var orderNumber = query.OrderNumber.Trim();
            ordersQuery = ordersQuery.Where(x => x.OrderNumber.Contains(orderNumber));
        }

        if (query.DateFromUtc.HasValue)
            ordersQuery = ordersQuery.Where(x => x.CreatedAtUtc >= query.DateFromUtc.Value);

        var normalizedDateToUtc = NormalizeCreatedToUtc(query.DateToUtc);

        if (normalizedDateToUtc.HasValue)
            ordersQuery = ordersQuery.Where(x => x.CreatedAtUtc < normalizedDateToUtc.Value);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        if (pageSize > 200) pageSize = 200;

        var totalCount = await ordersQuery.CountAsync();

        var pageOrderIds = await ordersQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync();

        var orders = await _context.Orders
            .Where(x => pageOrderIds.Contains(x.Id))
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .Include(x => x.CancelledByUser)
            .Include(x => x.Items)
                .ThenInclude(x => x.Product)
            .AsNoTracking()
            .ToListAsync();

        var orderById = orders.ToDictionary(x => x.Id);

        var items = pageOrderIds
            .Where(orderById.ContainsKey)
            .Select(id => MapOrder(orderById[id]))
            .ToList();

        return new OrderQueryResult<PagedResultDto<OrderResponseDto>>
        {
            Status = OrderQueryStatus.Success,
            Data = new PagedResultDto<OrderResponseDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        };
    }

    public async Task<OrderQueryResult<OrderResponseDto>> GetByIdAsync(
        Guid orderId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var isAdminOrManager =
            currentUserRoles.Contains(AppRoles.Admin) ||
            currentUserRoles.Contains(AppRoles.Manager);

        var order = await _context.Orders
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .Include(x => x.CancelledByUser)
            .Include(x => x.Items)
                .ThenInclude(x => x.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order is null)
        {
            return new OrderQueryResult<OrderResponseDto>
            {
                Status = OrderQueryStatus.OrderNotFound
            };
        }

        if (!isAdminOrManager && order.SalesRepId != currentUser.Id)
        {
            return new OrderQueryResult<OrderResponseDto>
            {
                Status = OrderQueryStatus.Forbidden
            };
        }

        return new OrderQueryResult<OrderResponseDto>
        {
            Status = OrderQueryStatus.Success,
            Data = MapOrder(order)
        };
    }

    public async Task<OrderQueryResult<IReadOnlyList<OrderActionHistoryResponseDto>>> GetHistoryAsync(
        Guid orderId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var isAdminOrManager =
            currentUserRoles.Contains(AppRoles.Admin) ||
            currentUserRoles.Contains(AppRoles.Manager);

        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order is null)
        {
            return new OrderQueryResult<IReadOnlyList<OrderActionHistoryResponseDto>>
            {
                Status = OrderQueryStatus.OrderNotFound
            };
        }

        if (!isAdminOrManager && order.SalesRepId != currentUser.Id)
        {
            return new OrderQueryResult<IReadOnlyList<OrderActionHistoryResponseDto>>
            {
                Status = OrderQueryStatus.Forbidden
            };
        }

        var history = await _context.OrderActionHistories
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.ActionAtUtc)
            .Select(x => new OrderActionHistoryResponseDto
            {
                Id = x.Id,
                OrderId = x.OrderId,
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

        return new OrderQueryResult<IReadOnlyList<OrderActionHistoryResponseDto>>
        {
            Status = OrderQueryStatus.Success,
            Data = history
        };
    }

    public async Task<OrderQueryResult<OrderOperationsReportResponseDto>> GetOperationsReportAsync(
        GetOrderOperationsReportQueryDto query)
    {
        var now = DateTime.UtcNow;
        var (fromUtc, toUtc) = NormalizeOperationsReportRange(query, now);

        var submissionsWindowQuery = _context.Orders
            .AsNoTracking()
            .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc < toUtc)
            .AsQueryable();

        var activeOrdersQuery = _context.Orders
            .AsNoTracking()
            .Where(x => x.Status != OrderStatus.Cancelled)
            .AsQueryable();

        var cancelledOrdersWindowQuery = _context.Orders
            .AsNoTracking()
            .Where(x =>
                x.Status == OrderStatus.Cancelled &&
                x.CancelledAtUtc.HasValue &&
                x.CancelledAtUtc.Value >= fromUtc &&
                x.CancelledAtUtc.Value < toUtc)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
        {
            submissionsWindowQuery = submissionsWindowQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);
            activeOrdersQuery = activeOrdersQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);
            cancelledOrdersWindowQuery = cancelledOrdersWindowQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);
        }

        if (query.CustomerId.HasValue)
        {
            submissionsWindowQuery = submissionsWindowQuery.Where(x => x.CustomerId == query.CustomerId.Value);
            activeOrdersQuery = activeOrdersQuery.Where(x => x.CustomerId == query.CustomerId.Value);
            cancelledOrdersWindowQuery = cancelledOrdersWindowQuery.Where(x => x.CustomerId == query.CustomerId.Value);
        }

        var submissionRows = await submissionsWindowQuery
            .Select(x => new SubmissionWindowRow
            {
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                Amount = x.TotalAmount,
                PaymentType = x.PaymentType,
                Status = x.Status
            })
            .ToListAsync();

        var activeRows = await activeOrdersQuery
            .Select(x => new ActiveOrderRow
            {
                CustomerId = x.CustomerId,
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                Amount = x.TotalAmount,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        var cancelledRows = await cancelledOrdersWindowQuery
            .Select(x => new CancelledOrderRow
            {
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                Amount = x.TotalAmount,
                CreatedAtUtc = x.CreatedAtUtc,
                CancelledAtUtc = x.CancelledAtUtc!.Value
            })
            .ToListAsync();

        var activeSnapshot = BuildActiveSnapshot(activeRows, now, query.StaleAfterHours);
        var throughputSummary = BuildThroughputSummary(submissionRows, cancelledRows);
        var activeAgingBuckets = BuildActiveAgingBuckets(activeRows, now);

        var paymentTypeBreakdown = submissionRows
            .GroupBy(x => x.PaymentType)
            .OrderBy(g => g.Key)
            .Select(g => new OrderPaymentTypeBreakdownDto
            {
                PaymentType = g.Key,
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount)
            })
            .ToList();

        var salesRepPerformance = BuildSalesRepPerformance(submissionRows, activeRows, cancelledRows);

        return new OrderQueryResult<OrderOperationsReportResponseDto>
        {
            Status = OrderQueryStatus.Success,
            Data = new OrderOperationsReportResponseDto
            {
                GeneratedAtUtc = now,
                RangeFromUtc = fromUtc,
                RangeToUtc = toUtc,
                StaleAfterHours = query.StaleAfterHours,
                ActiveSnapshot = activeSnapshot,
                ThroughputSummary = throughputSummary,
                ActiveOrderAgingBuckets = activeAgingBuckets,
                PaymentTypeBreakdown = paymentTypeBreakdown,
                SalesRepPerformance = salesRepPerformance
            }
        };
    }

    private static OrderOperationsActiveSnapshotDto BuildActiveSnapshot(
        IReadOnlyCollection<ActiveOrderRow> rows,
        DateTime now,
        int staleAfterHours)
    {
        if (rows.Count == 0)
            return new OrderOperationsActiveSnapshotDto();

        var staleRows = rows
            .Where(x => (now - x.CreatedAtUtc).TotalHours >= staleAfterHours)
            .ToList();

        return new OrderOperationsActiveSnapshotDto
        {
            ActiveOrdersCount = rows.Count,
            ActiveOrdersAmount = rows.Sum(x => x.Amount),
            CustomersWithActiveOrdersCount = rows.Select(x => x.CustomerId).Distinct().Count(),
            SalesRepsWithActiveOrdersCount = rows.Select(x => x.SalesRepId).Distinct().Count(),
            StaleActiveOrdersCount = staleRows.Count,
            StaleActiveOrdersAmount = staleRows.Sum(x => x.Amount),
            AverageActiveOrderAgeInHours = Math.Round(rows.Average(x => (now - x.CreatedAtUtc).TotalHours), 2),
            OldestActiveOrderAgeInHours = Math.Round(rows.Max(x => (now - x.CreatedAtUtc).TotalHours), 2)
        };
    }

    private static OrderOperationsThroughputSummaryDto BuildThroughputSummary(
        IReadOnlyCollection<SubmissionWindowRow> submissionRows,
        IReadOnlyCollection<CancelledOrderRow> cancelledRows)
    {
        var submittedCount = submissionRows.Count;
        var cancelledCount = cancelledRows.Count;

        return new OrderOperationsThroughputSummaryDto
        {
            SubmittedCount = submittedCount,
            SubmittedAmount = submissionRows.Sum(x => x.Amount),
            CancelledCount = cancelledCount,
            CancelledAmount = cancelledRows.Sum(x => x.Amount),
            CancellationRatePercent = submittedCount == 0
                ? null
                : Math.Round((double)cancelledCount / submittedCount * 100d, 2),
            AverageCancellationTurnaroundHours = cancelledCount == 0
                ? null
                : Math.Round(cancelledRows.Average(x => (x.CancelledAtUtc - x.CreatedAtUtc).TotalHours), 2)
        };
    }

    private static List<OrderActiveAgingBucketDto> BuildActiveAgingBuckets(
        IReadOnlyCollection<ActiveOrderRow> rows,
        DateTime now)
    {
        return
        [
            BuildAgingBucket(rows, now, "0-24h", 0, 24),
            BuildAgingBucket(rows, now, "24-48h", 24, 48),
            BuildAgingBucket(rows, now, "48-72h", 48, 72),
            BuildAgingBucket(rows, now, "72h+", 72, null)
        ];
    }

    private static OrderActiveAgingBucketDto BuildAgingBucket(
        IReadOnlyCollection<ActiveOrderRow> rows,
        DateTime now,
        string label,
        double minHoursInclusive,
        double? maxHoursExclusive)
    {
        var matchingRows = rows
            .Where(x =>
            {
                var age = (now - x.CreatedAtUtc).TotalHours;
                return age >= minHoursInclusive &&
                       (!maxHoursExclusive.HasValue || age < maxHoursExclusive.Value);
            })
            .ToList();

        return new OrderActiveAgingBucketDto
        {
            Label = label,
            Count = matchingRows.Count,
            Amount = matchingRows.Sum(x => x.Amount)
        };
    }

    private static List<OrderSalesRepPerformanceDto> BuildSalesRepPerformance(
        IReadOnlyCollection<SubmissionWindowRow> submissionRows,
        IReadOnlyCollection<ActiveOrderRow> activeRows,
        IReadOnlyCollection<CancelledOrderRow> cancelledRows)
    {
        var salesRepIds = submissionRows.Select(x => x.SalesRepId)
            .Concat(activeRows.Select(x => x.SalesRepId))
            .Concat(cancelledRows.Select(x => x.SalesRepId))
            .Distinct()
            .ToList();

        var result = new List<OrderSalesRepPerformanceDto>();

        foreach (var salesRepId in salesRepIds)
        {
            var repSubmissions = submissionRows.Where(x => x.SalesRepId == salesRepId).ToList();
            var repActive = activeRows.Where(x => x.SalesRepId == salesRepId).ToList();
            var repCancelled = cancelledRows.Where(x => x.SalesRepId == salesRepId).ToList();

            var salesRepName =
                repSubmissions.Select(x => x.SalesRepName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ??
                repActive.Select(x => x.SalesRepName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ??
                repCancelled.Select(x => x.SalesRepName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ??
                "Unknown Sales Rep";

            result.Add(new OrderSalesRepPerformanceDto
            {
                SalesRepId = salesRepId,
                SalesRepName = salesRepName,
                SubmittedCount = repSubmissions.Count,
                SubmittedAmount = repSubmissions.Sum(x => x.Amount),
                CancelledCount = repCancelled.Count,
                CancelledAmount = repCancelled.Sum(x => x.Amount),
                ActiveOrdersCount = repActive.Count,
                ActiveOrdersAmount = repActive.Sum(x => x.Amount)
            });
        }

        return result
            .OrderByDescending(x => x.SubmittedAmount)
            .ThenBy(x => x.SalesRepName)
            .ToList();
    }

    private static (DateTime FromUtc, DateTime ToUtc) NormalizeOperationsReportRange(
        GetOrderOperationsReportQueryDto query,
        DateTime now)
    {
        var toUtc = query.DateToUtc ?? now;
        if (toUtc.TimeOfDay == TimeSpan.Zero)
            toUtc = toUtc.Date.AddDays(1);

        var fromUtc = query.DateFromUtc ?? toUtc.AddDays(-7);

        return (fromUtc, toUtc);
    }


    private static DateTime? NormalizeCreatedToUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.TimeOfDay == TimeSpan.Zero
            ? value.Value.Date.AddDays(1)
            : value.Value;
    }

    private static OrderResponseDto MapOrder(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            VisitId = order.VisitId,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer.Name,
            SalesRepId = order.SalesRepId,
            SalesRepName = order.SalesRep.FullName,
            PaymentType = order.PaymentType,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Notes = order.Notes,
            CancelledByUserId = order.CancelledByUserId,
            CancelledByUserName = order.CancelledByUser?.FullName,
            CancelledAtUtc = order.CancelledAtUtc,
            CancellationReason = order.CancellationReason,
            RowVersion = RowVersionTokenHelper.Encode(order.RowVersion),
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            Items = order.Items.Select(item => new OrderItemResponseDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            }).ToList()
        };
    }

    private sealed class ActiveOrderRow
    {
        public Guid CustomerId { get; init; }
        public Guid SalesRepId { get; init; }
        public string SalesRepName { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }

    private sealed class SubmissionWindowRow
    {
        public Guid SalesRepId { get; init; }
        public string SalesRepName { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public PaymentType PaymentType { get; init; }
        public OrderStatus Status { get; init; }
    }

    private sealed class CancelledOrderRow
    {
        public Guid SalesRepId { get; init; }
        public string SalesRepName { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public DateTime CancelledAtUtc { get; init; }
    }
}