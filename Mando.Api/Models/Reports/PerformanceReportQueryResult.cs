using Mando.Api.Enums;

namespace Mando.Api.Models.Reports;

public sealed class PerformanceReportQueryResult<T>
{
    public PerformanceReportQueryStatus Status { get; init; }
    public T? Data { get; init; }
    public string? ValidationMessage { get; init; }
}