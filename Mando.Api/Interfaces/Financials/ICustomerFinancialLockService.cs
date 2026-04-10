namespace Mando.Api.Interfaces.Financials;

public interface ICustomerFinancialLockService
{
    Task<bool> LockAsync(Guid customerId, CancellationToken cancellationToken = default);
}