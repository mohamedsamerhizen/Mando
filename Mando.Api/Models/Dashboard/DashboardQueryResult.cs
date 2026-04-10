using Mando.Api.Enums;

namespace Mando.Api.Models.Dashboard;

public sealed class DashboardQueryResult<T>
{
    public DashboardQueryStatus Status { get; init; }
    public T? Data { get; init; }
}