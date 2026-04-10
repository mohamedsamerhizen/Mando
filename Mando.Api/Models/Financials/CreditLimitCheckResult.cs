namespace Mando.Api.Models.Financials;

public sealed class CreditLimitCheckResult
{
    public bool Allowed { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal ProjectedBalance { get; init; }
    public decimal CreditLimit { get; init; }
}