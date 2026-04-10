using Mando.Api.DTOs.Operations;
using Mando.Api.Enums;

namespace Mando.Api.Models.Operations;

public sealed class OperationsAlertReviewWorkflowResult
{
    public OperationsAlertReviewWorkflowStatus Status { get; init; }
    public OperationsAlertReviewDto? Review { get; init; }
}