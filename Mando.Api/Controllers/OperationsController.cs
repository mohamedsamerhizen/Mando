using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.Common;
using Mando.Api.DTOs.Operations;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Operations;
using Mando.Api.Models.Operations;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/operations")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
public class OperationsController : CurrentUserAwareControllerBase
{
    private readonly IOperationsQueryService _operationsQueryService;
    private readonly IOperationsAlertWorkflowService _operationsAlertWorkflowService;

    public OperationsController(
        ICurrentUserContext currentUserContext,
        IOperationsQueryService operationsQueryService,
        IOperationsAlertWorkflowService operationsAlertWorkflowService)
        : base(currentUserContext)
    {
        _operationsQueryService = operationsQueryService;
        _operationsAlertWorkflowService = operationsAlertWorkflowService;
    }

    [HttpGet("dashboard/today")]
    public async Task<ActionResult<OperationsDashboardResponseDto>> GetTodayDashboard(
        [FromQuery] Guid? salesRepId,
        [FromQuery] Guid? customerId,
        [FromQuery] VisitStatus? visitStatus,
        [FromQuery] PaymentStatus? paymentStatus,
        [FromQuery] bool includeVisits = true,
        [FromQuery] bool includeOrders = true,
        [FromQuery] bool includePayments = true,
        [FromQuery] int itemsLimit = 50)
    {
        var result = await _operationsQueryService.GetTodayDashboardAsync(
            salesRepId,
            customerId,
            visitStatus,
            paymentStatus,
            includeVisits,
            includeOrders,
            includePayments,
            itemsLimit);

        return MapResult(result);
    }

    [HttpGet("dashboard/range")]
    public async Task<ActionResult<OperationsDashboardResponseDto>> GetRangeDashboard(
        [FromQuery] GetOperationsDashboardQueryDto query)
    {
        var result = await _operationsQueryService.GetRangeDashboardAsync(query);
        return MapResult(result);
    }

    [HttpGet("dashboard/unified")]
    public async Task<ActionResult<UnifiedOperationsDashboardResponseDto>> GetUnifiedDashboard(
        [FromQuery] GetUnifiedOperationsDashboardQueryDto query)
    {
        var result = await _operationsQueryService.GetUnifiedDashboardAsync(query);
        return MapResult(result);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<OperationsAlertsResponseDto>> GetAlerts(
        [FromQuery] GetOperationsAlertsQueryDto query)
    {
        var result = await _operationsQueryService.GetAlertsAsync(query);
        return MapResult(result);
    }

    [HttpGet("alerts/reviews")]
    public async Task<ActionResult<IReadOnlyList<OperationsAlertReviewDto>>> GetAlertReviewHistory(
        [FromQuery] string alertFingerprint)
    {
        var result = await _operationsQueryService.GetAlertReviewHistoryAsync(alertFingerprint);
        return MapResult(result);
    }

    [HttpPost("alerts/reviews")]
    public async Task<ActionResult<OperationsAlertReviewDto>> ReviewAlert(
        ReviewOperationsAlertRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _operationsAlertWorkflowService.ReviewAsync(request, currentUser);

        return result.Status switch
        {
            OperationsAlertReviewWorkflowStatus.Success => Ok(result.Review),
            OperationsAlertReviewWorkflowStatus.AlertFingerprintRequired => ApiResponseFactory.BadRequest(this, "alert_fingerprint_required", "AlertFingerprint is required."),
            OperationsAlertReviewWorkflowStatus.InvalidAlertFingerprint => ApiResponseFactory.BadRequest(this, "invalid_alert_fingerprint", "AlertFingerprint is invalid."),
            OperationsAlertReviewWorkflowStatus.InvalidReviewStatus => ApiResponseFactory.BadRequest(this, "invalid_alert_review_status", "Status must be Acknowledged, Resolved, or Dismissed."),
            OperationsAlertReviewWorkflowStatus.ReviewCommentRequired => ApiResponseFactory.BadRequest(this, "review_comment_required", "Resolved and dismissed alerts require a non-empty comment."),
            OperationsAlertReviewWorkflowStatus.AlertNotFound => ApiResponseFactory.NotFound(this, "alert_not_found", "Alert was not found or is no longer active for this occurrence."),
            OperationsAlertReviewWorkflowStatus.AlertAlreadyClosed => ApiResponseFactory.Conflict(this, "alert_already_closed", "This alert occurrence is already closed and cannot be reviewed again."),
            OperationsAlertReviewWorkflowStatus.AlertAlreadyInRequestedState => ApiResponseFactory.Conflict(this, "alert_already_in_requested_state", "This alert occurrence is already in the requested review state."),
            _ => Problem("Unexpected operations alert review result.")
        };
    }

    [HttpGet("kpis/today")]
    public async Task<ActionResult<OperationsKpiDashboardResponseDto>> GetTodayKpis(
        [FromQuery] int topCount = 5)
    {
        var result = await _operationsQueryService.GetTodayKpisAsync(topCount);
        return MapResult(result);
    }

    [HttpGet("kpis/range")]
    public async Task<ActionResult<OperationsKpiDashboardResponseDto>> GetRangeKpis(
        [FromQuery] GetOperationsKpiQueryDto query)
    {
        var result = await _operationsQueryService.GetRangeKpisAsync(query);
        return MapResult(result);
    }

    private ActionResult<T> MapResult<T>(OperationsQueryResult<T> result)
    {
        return result.Status switch
        {
            OperationsQueryStatus.Success => Ok(result.Data),
            OperationsQueryStatus.ValidationError => ApiResponseFactory.BadRequest(this, "validation_error", result.ValidationMessage ?? "The request is invalid."),
            _ => new ActionResult<T>(Problem("Unexpected operations query result."))
        };
    }
}