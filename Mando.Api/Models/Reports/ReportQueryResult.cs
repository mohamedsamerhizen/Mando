using Mando.Api.Enums;

namespace Mando.Api.Models.Reports;

public sealed class ReportQueryResult<T>
{
    public ReportQueryStatus Status { get; init; }
    public T? Data { get; init; }
}