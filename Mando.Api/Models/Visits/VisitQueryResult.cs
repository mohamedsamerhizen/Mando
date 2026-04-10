using Mando.Api.Enums;

namespace Mando.Api.Models.Visits;

public sealed class VisitQueryResult<T>
{
    public VisitQueryStatus Status { get; init; }
    public T? Data { get; init; }
}