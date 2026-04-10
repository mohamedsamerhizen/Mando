using Microsoft.EntityFrameworkCore;
using Mando.Api.Data;
using Mando.Api.DTOs.Reports;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Interfaces.Reports;
using Mando.Api.Models.Reports;

namespace Mando.Api.Services.Reports;

public class ReportQueryService : IReportQueryService
{
    private readonly AppDbContext _context;
    private readonly ICustomerBalanceService _customerBalanceService;

    public ReportQueryService(
        AppDbContext context,
        ICustomerBalanceService customerBalanceService)
    {
        _context = context;
        _customerBalanceService = customerBalanceService;
    }

    public async Task<ReportQueryResult<List<SalesByRepDto>>> GetSalesByRepAsync(GetReportDateRangeQueryDto query)
    {
        var ordersQuery = _context.Orders
            .Include(x => x.SalesRep)
            .Where(x => x.Status != OrderStatus.Cancelled)
            .AsQueryable();

        if (query.DateFromUtc.HasValue)
        {
            ordersQuery = ordersQuery.Where(x => x.CreatedAtUtc >= query.DateFromUtc.Value);
        }

        var normalizedOrdersDateToUtc = NormalizeDateToUtc(query.DateToUtc);

        if (normalizedOrdersDateToUtc.HasValue)
        {
            ordersQuery = ordersQuery.Where(x => x.CreatedAtUtc < normalizedOrdersDateToUtc.Value);
        }

        var result = await ordersQuery
            .GroupBy(x => new { x.SalesRepId, x.SalesRep.FullName })
            .Select(g => new SalesByRepDto
            {
                SalesRepId = g.Key.SalesRepId,
                SalesRepName = g.Key.FullName,
                OrdersCount = g.Count(),
                TotalSales = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(x => x.TotalSales)
            .ToListAsync();

        return new ReportQueryResult<List<SalesByRepDto>>
        {
            Status = ReportQueryStatus.Success,
            Data = result
        };
    }

    public async Task<ReportQueryResult<List<CollectionsByRepDto>>> GetCollectionsByRepAsync(GetReportDateRangeQueryDto query)
    {
        var paymentsQuery = _context.Payments
            .Include(x => x.SalesRep)
            .Where(x => x.Status == PaymentStatus.Approved)
            .AsQueryable();

        if (query.DateFromUtc.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(x => x.ReviewedAtUtc >= query.DateFromUtc.Value);
        }

        var normalizedPaymentsDateToUtc = NormalizeDateToUtc(query.DateToUtc);

        if (normalizedPaymentsDateToUtc.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(x => x.ReviewedAtUtc.HasValue && x.ReviewedAtUtc.Value < normalizedPaymentsDateToUtc.Value);
        }

        var result = await paymentsQuery
            .GroupBy(x => new { x.SalesRepId, x.SalesRep.FullName })
            .Select(g => new CollectionsByRepDto
            {
                SalesRepId = g.Key.SalesRepId,
                SalesRepName = g.Key.FullName,
                ApprovedPaymentsCount = g.Count(),
                TotalCollections = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.TotalCollections)
            .ToListAsync();

        return new ReportQueryResult<List<CollectionsByRepDto>>
        {
            Status = ReportQueryStatus.Success,
            Data = result
        };
    }

    public async Task<ReportQueryResult<List<CustomerBalanceReportDto>>> GetCustomerBalancesAsync()
    {
        var result = await BuildCustomerBalanceReportAsync(topDebtOnly: false);

        return new ReportQueryResult<List<CustomerBalanceReportDto>>
        {
            Status = ReportQueryStatus.Success,
            Data = result
        };
    }

    public async Task<ReportQueryResult<List<CustomerBalanceReportDto>>> GetTopDebtCustomersAsync()
    {
        var result = await BuildCustomerBalanceReportAsync(topDebtOnly: true);

        return new ReportQueryResult<List<CustomerBalanceReportDto>>
        {
            Status = ReportQueryStatus.Success,
            Data = result
        };
    }

    public async Task<ReportQueryResult<List<VisitAttemptReportDto>>> GetVisitAttemptsAsync()
    {
        var result = await _context.VisitAttemptLogs
            .Include(x => x.SalesRep)
            .Include(x => x.Customer)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new VisitAttemptReportDto
            {
                VisitAttemptLogId = x.Id,
                SalesRepName = x.SalesRep.FullName,
                CustomerName = x.Customer.Name,
                AccuracyInMeters = x.AccuracyInMeters,
                DistanceFromCustomerInMeters = x.DistanceFromCustomerInMeters,
                ComplianceStatus = x.ComplianceStatus,
                IsSuccessful = x.IsSuccessful,
                Reason = x.Reason,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return new ReportQueryResult<List<VisitAttemptReportDto>>
        {
            Status = ReportQueryStatus.Success,
            Data = result
        };
    }

    public async Task<ReportQueryResult<List<SalesRepVisitComplianceDto>>> GetSalesRepsVisitComplianceAsync()
    {
        var result = await _context.VisitAttemptLogs
            .Include(x => x.SalesRep)
            .GroupBy(x => new { x.SalesRepId, x.SalesRep.FullName })
            .Select(g => new SalesRepVisitComplianceDto
            {
                SalesRepId = g.Key.SalesRepId,
                SalesRepName = g.Key.FullName,
                TotalAttempts = g.Count(),
                SuccessfulAttempts = g.Count(x => x.IsSuccessful),
                FailedAttempts = g.Count(x => !x.IsSuccessful),
                OutOfRangeAttempts = g.Count(x => x.ComplianceStatus == VisitComplianceStatus.OutOfRange),
                WeakAccuracyAttempts = g.Count(x => x.ComplianceStatus == VisitComplianceStatus.WeakAccuracy),
                AverageDistanceFromCustomerInMeters = g.Average(x => x.DistanceFromCustomerInMeters)
            })
            .OrderByDescending(x => x.FailedAttempts)
            .ThenByDescending(x => x.OutOfRangeAttempts)
            .ToListAsync();

        return new ReportQueryResult<List<SalesRepVisitComplianceDto>>
        {
            Status = ReportQueryStatus.Success,
            Data = result
        };
    }

    private async Task<List<CustomerBalanceReportDto>> BuildCustomerBalanceReportAsync(bool topDebtOnly)
    {
        var customers = await _context.Customers
            .Include(x => x.AssignedSalesRep)
            .AsNoTracking()
            .ToListAsync();

        var balanceSnapshots = await _customerBalanceService.GetSnapshotsAsync(
            customers.Select(x => x.Id).ToList());

        var result = customers
            .Select(customer =>
            {
                balanceSnapshots.TryGetValue(customer.Id, out var snapshot);

                return new CustomerBalanceReportDto
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    CustomerCode = customer.Code,
                    SalesRepName = customer.AssignedSalesRep.FullName,
                    OpeningBalance = snapshot?.OpeningBalance ?? customer.OpeningBalance,
                    TotalOrders = snapshot?.TotalOrders ?? 0m,
                    ApprovedPayments = snapshot?.ApprovedPayments ?? 0m,
                    CurrentBalance = snapshot?.CurrentBalance ?? customer.OpeningBalance
                };
            });

        if (topDebtOnly)
        {
            return result
                .Where(x => x.CurrentBalance > 0)
                .OrderByDescending(x => x.CurrentBalance)
                .Take(10)
                .ToList();
        }

        return result
            .OrderBy(x => x.CustomerName)
            .ToList();
    }


    private static DateTime? NormalizeDateToUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.TimeOfDay == TimeSpan.Zero
            ? value.Value.Date.AddDays(1)
            : value.Value;
    }

}