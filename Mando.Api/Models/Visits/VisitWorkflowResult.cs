using Mando.Api.Entities;
using Mando.Api.Enums;

namespace Mando.Api.Models.Visits;

public sealed class VisitWorkflowResult
{
    public VisitWorkflowStatus Status { get; init; }
    public Visit? Visit { get; init; }
    public double DistanceFromCustomerInMeters { get; init; }
    public double MaxAllowedAccuracyMeters { get; init; }
    public double MaxStartVisitDistanceMeters { get; init; }
    public double MaxEndVisitDistanceMeters { get; init; }
}
