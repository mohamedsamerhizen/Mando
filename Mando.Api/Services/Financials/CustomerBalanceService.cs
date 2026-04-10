using Microsoft.EntityFrameworkCore;
using Mando.Api.Data;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Models.Financials;

namespace Mando.Api.Services.Financials;

public class CustomerBalanceService : ICustomerBalanceService
{
    private readonly AppDbContext _context;

    public CustomerBalanceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerBalanceSnapshot?> GetSnapshotAsync(Guid customerId)
    {
        var snapshots = await GetSnapshotsAsync([customerId]);
        return snapshots.GetValueOrDefault(customerId);
    }

    public async Task<IReadOnlyDictionary<Guid, CustomerBalanceSnapshot>> GetSnapshotsAsync(
        IReadOnlyCollection<Guid> customerIds)
    {
        if (customerIds.Count == 0)
            return new Dictionary<Guid, CustomerBalanceSnapshot>();

        var distinctCustomerIds = customerIds
            .Distinct()
            .ToList();

        var customers = await _context.Customers
            .Where(x => distinctCustomerIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.OpeningBalance,
                x.CreditLimit
            })
            .ToListAsync();

        if (customers.Count == 0)
            return new Dictionary<Guid, CustomerBalanceSnapshot>();

        var existingCustomerIds = customers
            .Select(x => x.Id)
            .ToList();

        var orderTotalsByCustomerId = await _context.Orders
            .Where(x =>
                existingCustomerIds.Contains(x.CustomerId) &&
                x.Status != OrderStatus.Cancelled)
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalOrders = g.Sum(x => x.TotalAmount)
            })
            .ToDictionaryAsync(x => x.CustomerId, x => x.TotalOrders);

        var approvedPaymentsByCustomerId = await _context.Payments
            .Where(x =>
                existingCustomerIds.Contains(x.CustomerId) &&
                x.Status == PaymentStatus.Approved)
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                ApprovedPayments = g.Sum(x => x.Amount)
            })
            .ToDictionaryAsync(x => x.CustomerId, x => x.ApprovedPayments);

        var result = new Dictionary<Guid, CustomerBalanceSnapshot>(customers.Count);

        foreach (var customer in customers)
        {
            orderTotalsByCustomerId.TryGetValue(customer.Id, out var totalOrders);
            approvedPaymentsByCustomerId.TryGetValue(customer.Id, out var approvedPayments);

            result[customer.Id] = new CustomerBalanceSnapshot
            {
                CustomerId = customer.Id,
                OpeningBalance = customer.OpeningBalance,
                TotalOrders = totalOrders,
                ApprovedPayments = approvedPayments,
                CurrentBalance = customer.OpeningBalance + totalOrders - approvedPayments,
                CreditLimit = customer.CreditLimit
            };
        }

        return result;
    }

    public async Task<CreditLimitCheckResult> CheckCreditLimitAsync(Guid customerId, decimal newOrderAmount)
    {
        var snapshot = await GetSnapshotAsync(customerId)
            ?? throw new InvalidOperationException($"Customer '{customerId}' was not found while checking credit limit.");

        var projectedBalance = snapshot.CurrentBalance + newOrderAmount;

        return new CreditLimitCheckResult
        {
            Allowed = projectedBalance <= snapshot.CreditLimit,
            CurrentBalance = snapshot.CurrentBalance,
            ProjectedBalance = projectedBalance,
            CreditLimit = snapshot.CreditLimit
        };
    }
}