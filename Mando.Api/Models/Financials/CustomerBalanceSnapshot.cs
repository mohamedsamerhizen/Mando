namespace Mando.Api.Models.Financials;

public sealed class CustomerBalanceSnapshot
{
    public Guid CustomerId { get; init; }
    public decimal OpeningBalance { get; init; }
    public decimal TotalOrders { get; init; }
    public decimal ApprovedPayments { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal CreditLimit { get; init; }
}