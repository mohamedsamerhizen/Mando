using Mando.Api.Enums;

namespace Mando.Api.Models.Payments;

public sealed class PaymentQueryResult<T>
{
    public PaymentQueryStatus Status { get; init; }
    public T? Data { get; init; }
}