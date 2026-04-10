using Mando.Api.Enums;

namespace Mando.Api.Models.Orders;

public sealed class OrderQueryResult<T>
{
    public OrderQueryStatus Status { get; init; }
    public T? Data { get; init; }
}