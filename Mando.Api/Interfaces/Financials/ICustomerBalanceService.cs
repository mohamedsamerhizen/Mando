using Mando.Api.Models.Financials;

namespace Mando.Api.Interfaces.Financials;

public interface ICustomerBalanceService
{
    Task<CustomerBalanceSnapshot?> GetSnapshotAsync(Guid customerId);

    Task<IReadOnlyDictionary<Guid, CustomerBalanceSnapshot>> GetSnapshotsAsync(
        IReadOnlyCollection<Guid> customerIds);

    Task<CreditLimitCheckResult> CheckCreditLimitAsync(Guid customerId, decimal newOrderAmount);
}