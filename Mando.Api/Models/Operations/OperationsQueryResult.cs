using Mando.Api.Enums;

namespace Mando.Api.Models.Operations;

public sealed class OperationsQueryResult<T>
{
    public OperationsQueryStatus Status { get; init; }
    public T? Data { get; init; }
    public string? ValidationMessage { get; init; }
}