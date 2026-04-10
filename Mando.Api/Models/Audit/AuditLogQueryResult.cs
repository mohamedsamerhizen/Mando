using Mando.Api.Enums;

namespace Mando.Api.Models.Audit;

public sealed class AuditLogQueryResult<T>
{
    public AuditLogQueryStatus Status { get; init; }
    public T? Data { get; init; }
}