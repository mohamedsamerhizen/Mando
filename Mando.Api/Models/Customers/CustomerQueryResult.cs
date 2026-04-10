using Mando.Api.Enums;

namespace Mando.Api.Models.Customers;

public sealed class CustomerQueryResult<T>
{
    public CustomerQueryStatus Status { get; init; }
    public T? Data { get; init; }
}