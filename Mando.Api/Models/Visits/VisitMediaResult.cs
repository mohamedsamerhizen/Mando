using Mando.Api.Enums;

namespace Mando.Api.Models.Visits;

public sealed class VisitMediaResult<T>
{
    public VisitMediaStatus Status { get; init; }
    public T? Data { get; init; }
}