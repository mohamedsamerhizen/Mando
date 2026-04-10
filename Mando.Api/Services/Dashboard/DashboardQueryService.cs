using Microsoft.EntityFrameworkCore;
using Mando.Api.Data;
using Mando.Api.DTOs.Dashboard;
using Mando.Api.DTOs.Reports;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Dashboard;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Models.Dashboard;

namespace Mando.Api.Services.Dashboard;

public class DashboardQueryService : IDashboardQueryService
{
    private readonly AppDbContext _context;
    private readonly ICustomerBalanceService _customerBalanceService;

    public DashboardQueryService(
        AppDbContext context,
        ICustomerBalanceService customerBalanceService)
    {
        _context = context;
        _customerBalanceService = customerBalanceService;
    }

    public async Task<DashboardQueryResult<DashboardSummaryDto>> GetAdminSummaryAsync()
    {
        var totalCustomers = await _context.Customers.CountAsync();
        var activeCustomers = await _context.Customers.CountAsync(x => x.Status == CustomerStatus.Active);

        var totalProducts = await _context.Products.CountAsync();
        var activeProducts = await _context.Products.CountAsync(x => x.Status == ProductStatus.Active);

        var totalVisits = await _context.Visits.CountAsync();
        var inProgressVisits = await _context.Visits.CountAsync(x => x.Status == VisitStatus.InProgress);

        var totalOrders = await _context.Orders.CountAsync(x => x.Status != OrderStatus.Cancelled);
        var totalOrderAmount = await _context.Orders
            .Where(x => x.Status != OrderStatus.Cancelled)
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

        var totalPayments = await _context.Payments.CountAsync();
        var approvedPaymentsTotal = await _context.Payments
            .Where(x => x.Status == PaymentStatus.Approved)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var pendingPaymentsCount = await _context.Payments.CountAsync(x => x.Status == PaymentStatus.Pending);

        var recentVisits = await _context.Visits
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .OrderByDescending(x => x.CheckInAtUtc)
            .Take(5)
            .Select(x => new DashboardRecentVisitDto
            {
                VisitId = x.Id,
                CustomerName = x.Customer.Name,
                SalesRepName = x.SalesRep.FullName,
                CheckInAtUtc = x.CheckInAtUtc,
                Status = x.Status.ToString(),
                Outcome = x.Outcome.ToString()
            })
            .ToListAsync();

        var recentOrders = await _context.Orders
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new DashboardRecentOrderDto
            {
                OrderId = x.Id,
                OrderNumber = x.OrderNumber,
                CustomerName = x.Customer.Name,
                SalesRepName = x.SalesRep.FullName,
                TotalAmount = x.TotalAmount,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        var recentPayments = await _context.Payments
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new DashboardRecentPaymentDto
            {
                PaymentId = x.Id,
                PaymentNumber = x.PaymentNumber,
                CustomerName = x.Customer.Name,
                SalesRepName = x.SalesRep.FullName,
                Amount = x.Amount,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        var pendingPayments = await _context.Payments
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .Where(x => x.Status == PaymentStatus.Pending)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new DashboardRecentPaymentDto
            {
                PaymentId = x.Id,
                PaymentNumber = x.PaymentNumber,
                CustomerName = x.Customer.Name,
                SalesRepName = x.SalesRep.FullName,
                Amount = x.Amount,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        var customers = await _context.Customers
            .Include(x => x.AssignedSalesRep)
            .AsNoTracking()
            .ToListAsync();

        var balanceSnapshots = await _customerBalanceService.GetSnapshotsAsync(
            customers.Select(x => x.Id).ToList());

        var topDebtCustomers = customers
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
            })
            .Where(x => x.CurrentBalance > 0)
            .OrderByDescending(x => x.CurrentBalance)
            .Take(5)
            .ToList();

        var response = new DashboardSummaryDto
        {
            TotalCustomers = totalCustomers,
            ActiveCustomers = activeCustomers,
            TotalProducts = totalProducts,
            ActiveProducts = activeProducts,
            TotalVisits = totalVisits,
            InProgressVisits = inProgressVisits,
            TotalOrders = totalOrders,
            TotalOrderAmount = totalOrderAmount,
            TotalPayments = totalPayments,
            ApprovedPaymentsTotal = approvedPaymentsTotal,
            PendingPaymentsCount = pendingPaymentsCount,
            RecentVisits = recentVisits,
            RecentOrders = recentOrders,
            RecentPayments = recentPayments,
            PendingPayments = pendingPayments,
            TopDebtCustomers = topDebtCustomers
        };

        return new DashboardQueryResult<DashboardSummaryDto>
        {
            Status = DashboardQueryStatus.Success,
            Data = response
        };
    }

    public async Task<DashboardQueryResult<SalesRepDashboardSummaryDto>> GetSalesRepSummaryAsync(AppUser currentUser)
    {
        var myCustomersCount = await _context.Customers
            .CountAsync(x => x.AssignedSalesRepId == currentUser.Id);

        var myActiveCustomersCount = await _context.Customers
            .CountAsync(x => x.AssignedSalesRepId == currentUser.Id && x.Status == CustomerStatus.Active);

        var myVisitsCount = await _context.Visits
            .CountAsync(x => x.SalesRepId == currentUser.Id);

        var myInProgressVisitsCount = await _context.Visits
            .CountAsync(x => x.SalesRepId == currentUser.Id && x.Status == VisitStatus.InProgress);

        var myOrdersCount = await _context.Orders
            .CountAsync(x => x.SalesRepId == currentUser.Id && x.Status != OrderStatus.Cancelled);

        var myTotalSales = await _context.Orders
            .Where(x => x.SalesRepId == currentUser.Id && x.Status != OrderStatus.Cancelled)
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

        var myPaymentsCount = await _context.Payments
            .CountAsync(x => x.SalesRepId == currentUser.Id);

        var myApprovedPaymentsTotal = await _context.Payments
            .Where(x => x.SalesRepId == currentUser.Id && x.Status == PaymentStatus.Approved)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var myPendingPaymentsCount = await _context.Payments
            .CountAsync(x => x.SalesRepId == currentUser.Id && x.Status == PaymentStatus.Pending);

        var recentVisits = await _context.Visits
            .Include(x => x.Customer)
            .Where(x => x.SalesRepId == currentUser.Id)
            .OrderByDescending(x => x.CheckInAtUtc)
            .Take(5)
            .Select(x => new DashboardRecentVisitDto
            {
                VisitId = x.Id,
                CustomerName = x.Customer.Name,
                SalesRepName = currentUser.FullName,
                CheckInAtUtc = x.CheckInAtUtc,
                Status = x.Status.ToString(),
                Outcome = x.Outcome.ToString()
            })
            .ToListAsync();

        var recentOrders = await _context.Orders
            .Include(x => x.Customer)
            .Where(x => x.SalesRepId == currentUser.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new DashboardRecentOrderDto
            {
                OrderId = x.Id,
                OrderNumber = x.OrderNumber,
                CustomerName = x.Customer.Name,
                SalesRepName = currentUser.FullName,
                TotalAmount = x.TotalAmount,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        var recentPayments = await _context.Payments
            .Include(x => x.Customer)
            .Where(x => x.SalesRepId == currentUser.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new DashboardRecentPaymentDto
            {
                PaymentId = x.Id,
                PaymentNumber = x.PaymentNumber,
                CustomerName = x.Customer.Name,
                SalesRepName = currentUser.FullName,
                Amount = x.Amount,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        var response = new SalesRepDashboardSummaryDto
        {
            MyCustomersCount = myCustomersCount,
            MyActiveCustomersCount = myActiveCustomersCount,
            MyVisitsCount = myVisitsCount,
            MyInProgressVisitsCount = myInProgressVisitsCount,
            MyOrdersCount = myOrdersCount,
            MyTotalSales = myTotalSales,
            MyPaymentsCount = myPaymentsCount,
            MyApprovedPaymentsTotal = myApprovedPaymentsTotal,
            MyPendingPaymentsCount = myPendingPaymentsCount,
            RecentVisits = recentVisits,
            RecentOrders = recentOrders,
            RecentPayments = recentPayments
        };

        return new DashboardQueryResult<SalesRepDashboardSummaryDto>
        {
            Status = DashboardQueryStatus.Success,
            Data = response
        };
    }
}