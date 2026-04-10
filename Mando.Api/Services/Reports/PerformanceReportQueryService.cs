
using Microsoft.EntityFrameworkCore;
using Mando.Api.Data;
using Mando.Api.DTOs.Reports;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Interfaces.Reports;
using Mando.Api.Models.Reports;

namespace Mando.Api.Services.Reports;

public class PerformanceReportQueryService : IPerformanceReportQueryService
{
    private readonly AppDbContext _context;
    private readonly ICustomerBalanceService _customerBalanceService;

    public PerformanceReportQueryService(
        AppDbContext context,
        ICustomerBalanceService customerBalanceService)
    {
        _context = context;
        _customerBalanceService = customerBalanceService;
    }

    public async Task<PerformanceReportQueryResult<SalesRepPerformanceReportResponseDto>> GetSalesRepPerformanceAsync(
        GetSalesRepPerformanceReportQueryDto query)
    {
        var validationMessage = ValidateDateRange(query.DateFromUtc, query.DateToUtc);
        if (validationMessage is not null)
        {
            return new PerformanceReportQueryResult<SalesRepPerformanceReportResponseDto>
            {
                Status = PerformanceReportQueryStatus.ValidationError,
                ValidationMessage = validationMessage
            };
        }

        var dateFromUtc = query.DateFromUtc!.Value;
        var dateToUtc = NormalizeDateTo(query.DateToUtc!.Value);

        var salesRepsQuery = _context.Users
            .Where(x => x.IsActive)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            salesRepsQuery = salesRepsQuery.Where(x => x.Id == query.SalesRepId.Value);

        var salesReps = await salesRepsQuery
            .Select(x => new
            {
                x.Id,
                x.FullName
            })
            .ToListAsync();

        var items = new List<SalesRepPerformanceReportItemDto>();

        foreach (var salesRep in salesReps)
        {
            var visitsQuery = _context.Visits
                .Where(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.CheckInAtUtc >= dateFromUtc &&
                    x.CheckInAtUtc < dateToUtc);

            var ordersQuery = _context.Orders
                .Where(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.Status != OrderStatus.Cancelled &&
                    x.CreatedAtUtc >= dateFromUtc &&
                    x.CreatedAtUtc < dateToUtc);

            var paymentsQuery = _context.Payments
                .Where(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.CreatedAtUtc >= dateFromUtc &&
                    x.CreatedAtUtc < dateToUtc);

            var totalVisits = await visitsQuery.CountAsync();
            var completedVisits = await visitsQuery.CountAsync(x => x.Status == VisitStatus.Completed);
            var inProgressVisits = await visitsQuery.CountAsync(x => x.Status == VisitStatus.InProgress);
            var cancelledVisits = await visitsQuery.CountAsync(x => x.Status == VisitStatus.Cancelled);

            var totalOrders = await ordersQuery.CountAsync();
            var totalSalesAmount = await ordersQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            var totalPayments = await paymentsQuery.CountAsync();
            var approvedPaymentsCount = await paymentsQuery.CountAsync(x => x.Status == PaymentStatus.Approved);
            var rejectedPaymentsCount = await paymentsQuery.CountAsync(x => x.Status == PaymentStatus.Rejected);
            var approvedCollectionsAmount = await paymentsQuery
                .Where(x => x.Status == PaymentStatus.Approved)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var hasAnyActivity =
                totalVisits > 0 ||
                totalOrders > 0 ||
                totalPayments > 0;

            if (!hasAnyActivity)
                continue;

            items.Add(new SalesRepPerformanceReportItemDto
            {
                SalesRepId = salesRep.Id,
                SalesRepName = salesRep.FullName,
                TotalVisits = totalVisits,
                CompletedVisits = completedVisits,
                InProgressVisits = inProgressVisits,
                CancelledVisits = cancelledVisits,
                TotalOrders = totalOrders,
                TotalSalesAmount = totalSalesAmount,
                TotalPayments = totalPayments,
                ApprovedPaymentsCount = approvedPaymentsCount,
                RejectedPaymentsCount = rejectedPaymentsCount,
                ApprovedCollectionsAmount = approvedCollectionsAmount
            });
        }

        items = items
            .OrderByDescending(x => x.TotalSalesAmount)
            .ThenByDescending(x => x.ApprovedCollectionsAmount)
            .ThenByDescending(x => x.TotalVisits)
            .ToList();

        var response = new SalesRepPerformanceReportResponseDto
        {
            DateFromUtc = dateFromUtc,
            DateToUtc = dateToUtc,
            SalesRepId = query.SalesRepId,
            TotalSalesReps = items.Count,
            TotalVisits = items.Sum(x => x.TotalVisits),
            TotalOrders = items.Sum(x => x.TotalOrders),
            TotalSalesAmount = items.Sum(x => x.TotalSalesAmount),
            TotalPayments = items.Sum(x => x.TotalPayments),
            TotalApprovedPayments = items.Sum(x => x.ApprovedPaymentsCount),
            TotalRejectedPayments = items.Sum(x => x.RejectedPaymentsCount),
            TotalApprovedCollectionsAmount = items.Sum(x => x.ApprovedCollectionsAmount),
            Items = items
        };

        return new PerformanceReportQueryResult<SalesRepPerformanceReportResponseDto>
        {
            Status = PerformanceReportQueryStatus.Success,
            Data = response
        };
    }

    public async Task<PerformanceReportQueryResult<CustomerDebtReportResponseDto>> GetCustomerDebtReportAsync(
        GetCustomerDebtReportQueryDto query)
    {
        var validationMessage = ValidateDateRange(query.DateFromUtc, query.DateToUtc);
        if (validationMessage is not null)
        {
            return new PerformanceReportQueryResult<CustomerDebtReportResponseDto>
            {
                Status = PerformanceReportQueryStatus.ValidationError,
                ValidationMessage = validationMessage
            };
        }

        var dateFromUtc = query.DateFromUtc!.Value;
        var dateToUtc = NormalizeDateTo(query.DateToUtc!.Value);

        var customersQuery = _context.Customers
            .Include(x => x.AssignedSalesRep)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            customersQuery = customersQuery.Where(x => x.AssignedSalesRepId == query.SalesRepId.Value);

        var customers = await customersQuery
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.OpeningBalance,
                x.AssignedSalesRepId,
                AssignedSalesRepName = x.AssignedSalesRep.FullName
            })
            .ToListAsync();

        var items = new List<CustomerDebtReportItemDto>();

        foreach (var customer in customers)
        {
            var periodOrders = await _context.Orders
                .Where(x =>
                    x.CustomerId == customer.Id &&
                    x.Status != OrderStatus.Cancelled &&
                    x.CreatedAtUtc >= dateFromUtc &&
                    x.CreatedAtUtc < dateToUtc)
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

            var periodApprovedPayments = await _context.Payments
                .Where(x =>
                    x.CustomerId == customer.Id &&
                    x.Status == PaymentStatus.Approved &&
                    x.ReviewedAtUtc.HasValue &&
                    x.ReviewedAtUtc.Value >= dateFromUtc &&
                    x.ReviewedAtUtc.Value < dateToUtc)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var lastOrderDateUtc = await _context.Orders
                .Where(x =>
                    x.CustomerId == customer.Id &&
                    x.Status != OrderStatus.Cancelled)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => (DateTime?)x.CreatedAtUtc)
                .FirstOrDefaultAsync();

            var lastPaymentDateUtc = await _context.Payments
                .Where(x =>
                    x.CustomerId == customer.Id &&
                    x.Status == PaymentStatus.Approved &&
                    x.ReviewedAtUtc.HasValue)
                .OrderByDescending(x => x.ReviewedAtUtc)
                .Select(x => x.ReviewedAtUtc)
                .FirstOrDefaultAsync();

            var balanceSnapshot = await _customerBalanceService.GetSnapshotAsync(customer.Id);
            var currentBalance = balanceSnapshot?.CurrentBalance ?? customer.OpeningBalance;

            if (query.PositiveBalanceOnly && currentBalance <= 0)
                continue;

            items.Add(new CustomerDebtReportItemDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                CustomerCode = customer.Code,
                AssignedSalesRepId = customer.AssignedSalesRepId,
                AssignedSalesRepName = customer.AssignedSalesRepName,
                OpeningBalance = balanceSnapshot?.OpeningBalance ?? customer.OpeningBalance,
                TotalOrders = periodOrders,
                ApprovedPayments = periodApprovedPayments,
                CurrentBalance = currentBalance,
                LastOrderDateUtc = lastOrderDateUtc,
                LastPaymentDateUtc = lastPaymentDateUtc
            });
        }

        items = items
            .OrderByDescending(x => x.CurrentBalance)
            .ThenByDescending(x => x.TotalOrders)
            .ToList();

        var response = new CustomerDebtReportResponseDto
        {
            DateFromUtc = dateFromUtc,
            DateToUtc = dateToUtc,
            SalesRepId = query.SalesRepId,
            PositiveBalanceOnly = query.PositiveBalanceOnly,
            TotalCustomers = items.Count,
            CustomersWithDebtCount = items.Count(x => x.CurrentBalance > 0),
            TotalOpeningBalance = items.Sum(x => x.OpeningBalance),
            TotalOrders = items.Sum(x => x.TotalOrders),
            TotalApprovedPayments = items.Sum(x => x.ApprovedPayments),
            TotalCurrentBalance = items.Sum(x => x.CurrentBalance),
            Items = items
        };

        return new PerformanceReportQueryResult<CustomerDebtReportResponseDto>
        {
            Status = PerformanceReportQueryStatus.Success,
            Data = response
        };
    }

    public async Task<PerformanceReportQueryResult<VisitComplianceReportResponseDto>> GetVisitComplianceReportAsync(
        GetVisitComplianceReportQueryDto query)
    {
        var validationMessage = ValidateDateRange(query.DateFromUtc, query.DateToUtc);
        if (validationMessage is not null)
        {
            return new PerformanceReportQueryResult<VisitComplianceReportResponseDto>
            {
                Status = PerformanceReportQueryStatus.ValidationError,
                ValidationMessage = validationMessage
            };
        }

        var dateFromUtc = query.DateFromUtc!.Value;
        var dateToUtc = NormalizeDateTo(query.DateToUtc!.Value);

        var attemptsQuery = _context.VisitAttemptLogs
            .Include(x => x.SalesRep)
            .Include(x => x.Customer)
            .Where(x => x.CreatedAtUtc >= dateFromUtc && x.CreatedAtUtc < dateToUtc)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            attemptsQuery = attemptsQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);

        if (query.CustomerId.HasValue)
            attemptsQuery = attemptsQuery.Where(x => x.CustomerId == query.CustomerId.Value);

        if (query.ComplianceStatus.HasValue)
            attemptsQuery = attemptsQuery.Where(x => x.ComplianceStatus == query.ComplianceStatus.Value);

        if (query.IsSuccessful.HasValue)
            attemptsQuery = attemptsQuery.Where(x => x.IsSuccessful == query.IsSuccessful.Value);

        var items = await attemptsQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new VisitComplianceReportItemDto
            {
                Id = x.Id,
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.Name,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                AccuracyInMeters = x.AccuracyInMeters,
                DistanceFromCustomerInMeters = x.DistanceFromCustomerInMeters,
                ComplianceStatus = x.ComplianceStatus,
                IsSuccessful = x.IsSuccessful,
                Reason = x.Reason,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        var response = new VisitComplianceReportResponseDto
        {
            DateFromUtc = dateFromUtc,
            DateToUtc = dateToUtc,
            SalesRepId = query.SalesRepId,
            CustomerId = query.CustomerId,
            ComplianceStatus = query.ComplianceStatus,
            IsSuccessful = query.IsSuccessful,
            TotalAttempts = items.Count,
            SuccessfulAttempts = items.Count(x => x.IsSuccessful),
            FailedAttempts = items.Count(x => !x.IsSuccessful),
            CompliantCount = items.Count(x => x.ComplianceStatus == VisitComplianceStatus.Compliant),
            OutOfRangeCount = items.Count(x => x.ComplianceStatus == VisitComplianceStatus.OutOfRange),
            WeakAccuracyCount = items.Count(x => x.ComplianceStatus == VisitComplianceStatus.WeakAccuracy),
            Items = items
        };

        return new PerformanceReportQueryResult<VisitComplianceReportResponseDto>
        {
            Status = PerformanceReportQueryStatus.Success,
            Data = response
        };
    }

    public async Task<PerformanceReportQueryResult<CollectionsBySalesRepReportResponseDto>> GetCollectionsBySalesRepAsync(
        GetCollectionsBySalesRepReportQueryDto query)
    {
        var validationMessage = ValidateDateRange(query.DateFromUtc, query.DateToUtc);
        if (validationMessage is not null)
        {
            return new PerformanceReportQueryResult<CollectionsBySalesRepReportResponseDto>
            {
                Status = PerformanceReportQueryStatus.ValidationError,
                ValidationMessage = validationMessage
            };
        }

        var dateFromUtc = query.DateFromUtc!.Value;
        var dateToUtc = NormalizeDateTo(query.DateToUtc!.Value);

        var salesRepsQuery = _context.Users
            .Where(x => x.IsActive)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            salesRepsQuery = salesRepsQuery.Where(x => x.Id == query.SalesRepId.Value);

        var salesReps = await salesRepsQuery
            .Select(x => new
            {
                x.Id,
                x.FullName
            })
            .ToListAsync();

        var items = new List<CollectionsBySalesRepReportItemDto>();

        foreach (var salesRep in salesReps)
        {
            var paymentsQuery = _context.Payments
                .Where(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.CreatedAtUtc >= dateFromUtc &&
                    x.CreatedAtUtc < dateToUtc);

            var totalPaymentsCount = await paymentsQuery.CountAsync();
            if (totalPaymentsCount == 0)
                continue;

            var pendingPaymentsCount = await paymentsQuery.CountAsync(x => x.Status == PaymentStatus.Pending);
            var approvedPaymentsCount = await paymentsQuery.CountAsync(x => x.Status == PaymentStatus.Approved);
            var rejectedPaymentsCount = await paymentsQuery.CountAsync(x => x.Status == PaymentStatus.Rejected);

            var totalPaymentsAmount = await paymentsQuery.SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var approvedPaymentsAmount = await paymentsQuery
                .Where(x => x.Status == PaymentStatus.Approved)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var rejectedPaymentsAmount = await paymentsQuery
                .Where(x => x.Status == PaymentStatus.Rejected)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            items.Add(new CollectionsBySalesRepReportItemDto
            {
                SalesRepId = salesRep.Id,
                SalesRepName = salesRep.FullName,
                TotalPaymentsCount = totalPaymentsCount,
                PendingPaymentsCount = pendingPaymentsCount,
                ApprovedPaymentsCount = approvedPaymentsCount,
                RejectedPaymentsCount = rejectedPaymentsCount,
                TotalPaymentsAmount = totalPaymentsAmount,
                ApprovedPaymentsAmount = approvedPaymentsAmount,
                RejectedPaymentsAmount = rejectedPaymentsAmount
            });
        }

        items = items
            .OrderByDescending(x => x.ApprovedPaymentsAmount)
            .ThenByDescending(x => x.TotalPaymentsAmount)
            .ThenByDescending(x => x.ApprovedPaymentsCount)
            .ToList();

        var response = new CollectionsBySalesRepReportResponseDto
        {
            DateFromUtc = dateFromUtc,
            DateToUtc = dateToUtc,
            SalesRepId = query.SalesRepId,
            TotalSalesReps = items.Count,
            TotalPaymentsCount = items.Sum(x => x.TotalPaymentsCount),
            TotalPendingPaymentsCount = items.Sum(x => x.PendingPaymentsCount),
            TotalApprovedPaymentsCount = items.Sum(x => x.ApprovedPaymentsCount),
            TotalRejectedPaymentsCount = items.Sum(x => x.RejectedPaymentsCount),
            TotalPaymentsAmount = items.Sum(x => x.TotalPaymentsAmount),
            TotalApprovedPaymentsAmount = items.Sum(x => x.ApprovedPaymentsAmount),
            TotalRejectedPaymentsAmount = items.Sum(x => x.RejectedPaymentsAmount),
            Items = items
        };

        return new PerformanceReportQueryResult<CollectionsBySalesRepReportResponseDto>
        {
            Status = PerformanceReportQueryStatus.Success,
            Data = response
        };
    }

    private static string? ValidateDateRange(DateTime? dateFromUtc, DateTime? dateToUtc)
    {
        if (!dateFromUtc.HasValue)
            return "DateFromUtc is required.";

        if (!dateToUtc.HasValue)
            return "DateToUtc is required.";

        if (dateToUtc.Value < dateFromUtc.Value)
            return "DateToUtc must be greater than or equal to DateFromUtc.";

        return null;
    }

    private static DateTime NormalizeDateTo(DateTime dateToUtc)
    {
        return dateToUtc == dateToUtc.Date
            ? dateToUtc.Date.AddDays(1)
            : dateToUtc;
    }
}
