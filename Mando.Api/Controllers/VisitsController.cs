using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.Common;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Visits;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Visits;
using Mando.Api.Models.Visits;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VisitsController : CurrentUserAwareControllerBase
{
    private readonly IVisitWorkflowService _visitWorkflowService;
    private readonly IVisitQueryService _visitQueryService;
    private readonly IVisitMediaService _visitMediaService;

    public VisitsController(
        ICurrentUserContext currentUserContext,
        IVisitWorkflowService visitWorkflowService,
        IVisitQueryService visitQueryService,
        IVisitMediaService visitMediaService)
        : base(currentUserContext)
    {
        _visitWorkflowService = visitWorkflowService;
        _visitQueryService = visitQueryService;
        _visitMediaService = visitMediaService;
    }

    [HttpPost("start")]
    [Authorize(Roles = AppRoles.SalesRep)]
    public async Task<ActionResult<VisitResponseDto>> Start(StartVisitRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _visitWorkflowService.StartAsync(request, currentUser);

        return result.Status switch
        {
            VisitWorkflowStatus.Success => CreatedAtAction(
                nameof(GetById),
                new { id = result.Visit!.Id },
                MapVisit(result.Visit!)),

            VisitWorkflowStatus.CustomerNotFound => ApiResponseFactory.NotFound(
                this,
                "customer_not_found",
                "Customer was not found."),

            VisitWorkflowStatus.Forbidden => Forbid(),

            VisitWorkflowStatus.CustomerInactive => ApiResponseFactory.BadRequest(
                this,
                "customer_inactive",
                "Cannot start visit for inactive customer."),

            VisitWorkflowStatus.WeakLocationAccuracy => ApiResponseFactory.BadRequest(
                this,
                "weak_location_accuracy",
                $"Location accuracy is too weak. Max allowed accuracy is {result.MaxAllowedAccuracyMeters} meters."),

            VisitWorkflowStatus.OutOfRange => ApiResponseFactory.BadRequest(
                this,
                "visit_out_of_range",
                $"You are too far from customer location. Current distance is {result.DistanceFromCustomerInMeters:0.##} meters and max allowed distance is {result.MaxStartVisitDistanceMeters} meters."),

            VisitWorkflowStatus.ActiveVisitExists => ApiResponseFactory.BadRequest(
                this,
                "active_visit_exists",
                "Sales rep already has an active visit."),

            _ => Problem("Unexpected visit start workflow result.")
        };
    }

    [HttpPost("{id:guid}/end")]
    [Authorize(Roles = AppRoles.SalesRep)]
    public async Task<ActionResult<VisitResponseDto>> End(Guid id, EndVisitRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _visitWorkflowService.EndAsync(id, request, currentUser);

        return result.Status switch
        {
            VisitWorkflowStatus.Success => Ok(MapVisit(result.Visit!)),

            VisitWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is required and must be a valid Base64 value."),

            VisitWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "visit_concurrency_conflict",
                "Visit was changed by another user. Refresh and retry."),

            VisitWorkflowStatus.WeakLocationAccuracy => ApiResponseFactory.BadRequest(
                this,
                "weak_location_accuracy",
                $"Location accuracy is too weak. Max allowed accuracy is {result.MaxAllowedAccuracyMeters} meters."),

            VisitWorkflowStatus.OutOfRange => ApiResponseFactory.BadRequest(
                this,
                "visit_end_out_of_range",
                $"You are too far from customer location to end the visit. Current distance is {result.DistanceFromCustomerInMeters:0.##} meters and max allowed distance is {result.MaxEndVisitDistanceMeters} meters."),

            VisitWorkflowStatus.VisitNotFound => ApiResponseFactory.NotFound(
                this,
                "visit_not_found",
                "Visit was not found."),

            VisitWorkflowStatus.Forbidden => Forbid(),

            VisitWorkflowStatus.VisitNotInProgress => ApiResponseFactory.BadRequest(
                this,
                "visit_not_in_progress",
                "Visit is not in progress."),

            VisitWorkflowStatus.InvalidOutcome => ApiResponseFactory.BadRequest(
                this,
                "invalid_visit_outcome",
                "Completed visit must have a final non-pending outcome."),

            _ => Problem("Unexpected visit end workflow result.")
        };
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.SalesRep)]
    public async Task<ActionResult<VisitResponseDto>> Cancel(Guid id, CancelVisitRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _visitWorkflowService.CancelAsync(id, request, currentUser);

        return result.Status switch
        {
            VisitWorkflowStatus.Success => Ok(MapVisit(result.Visit!)),

            VisitWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is required and must be a valid Base64 value."),

            VisitWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "visit_concurrency_conflict",
                "Visit was changed by another user. Refresh and retry."),

            VisitWorkflowStatus.VisitNotFound => ApiResponseFactory.NotFound(
                this,
                "visit_not_found",
                "Visit was not found."),

            VisitWorkflowStatus.Forbidden => Forbid(),

            VisitWorkflowStatus.VisitNotInProgress => ApiResponseFactory.BadRequest(
                this,
                "visit_not_in_progress",
                "Only in-progress visits can be cancelled."),

            VisitWorkflowStatus.VisitHasOrders => ApiResponseFactory.BadRequest(
                this,
                "visit_has_orders",
                "Visit cannot be cancelled because it already has orders."),

            VisitWorkflowStatus.VisitHasPayments => ApiResponseFactory.BadRequest(
                this,
                "visit_has_payments",
                "Visit cannot be cancelled because it already has payments."),

            _ => Problem("Unexpected visit cancel workflow result.")
        };
    }

    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = AppRoles.SalesRep)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(VisitImageResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VisitImageResponseDto>> UploadImage(
        Guid id,
        [FromForm] UploadVisitImageRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _visitMediaService.UploadImageAsync(id, request.File, baseUrl, currentUser);

        return MapMediaResult(result);
    }

    [HttpGet("{id:guid}/images")]
    public async Task<ActionResult<IReadOnlyList<VisitImageResponseDto>>> GetImages(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var result = await _visitMediaService.GetImagesAsync(id, baseUrl, currentUser, currentUserRoles);

        return MapMediaListResult(result);
    }

    [HttpDelete("images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid imageId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _visitMediaService.DeleteImageAsync(imageId, currentUser, currentUserRoles);

        return MapDeleteMediaResult(result);
    }

    [HttpGet("images/{imageId:guid}/content")]
    public async Task<IActionResult> GetImageContent(Guid imageId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _visitMediaService.GetImageContentAsync(imageId, currentUser, currentUserRoles);

        return MapImageContentResult(result);
    }

    [HttpGet("operations-report")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<VisitOperationsReportResponseDto>> GetOperationsReport(
        [FromQuery] GetVisitOperationsReportQueryDto query)
    {
        if (query.DateToUtc.HasValue && query.DateFromUtc.HasValue && query.DateToUtc.Value < query.DateFromUtc.Value)
        {
            return ApiResponseFactory.BadRequest(
                this,
                "invalid_report_range",
                "DateToUtc must be greater than or equal to DateFromUtc.");
        }

        var result = await _visitQueryService.GetOperationsReportAsync(query);
        return MapQueryResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<VisitResponseDto>>> GetAll([FromQuery] GetVisitsQueryDto query)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _visitQueryService.GetAllAsync(query, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VisitResponseDto>> GetById(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _visitQueryService.GetByIdAsync(id, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<VisitActionHistoryResponseDto>>> GetHistory(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _visitQueryService.GetHistoryAsync(id, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<ActionResult<VisitTimelineResponseDto>> GetTimeline(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _visitQueryService.GetTimelineAsync(id, baseUrl, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    private ActionResult<T> MapQueryResult<T>(VisitQueryResult<T> result)
    {
        switch (result.Status)
        {
            case VisitQueryStatus.Success:
                return Ok(result.Data);

            case VisitQueryStatus.VisitNotFound:
                return new ActionResult<T>(ApiResponseFactory.NotFound(
                    this,
                    "visit_not_found",
                    "Visit was not found."));

            case VisitQueryStatus.Forbidden:
                return new ActionResult<T>(Forbid());

            default:
                return new ActionResult<T>(Problem("Unexpected visit query result."));
        }
    }

    private ActionResult<VisitImageResponseDto> MapMediaResult(VisitMediaResult<VisitImageResponseDto> result)
    {
        return result.Status switch
        {
            VisitMediaStatus.Success => CreatedAtAction(
                nameof(GetImageContent),
                new { imageId = result.Data!.Id },
                result.Data),

            VisitMediaStatus.VisitNotFound => ApiResponseFactory.NotFound(
                this,
                "visit_not_found",
                "Visit was not found."),

            VisitMediaStatus.Forbidden => Forbid(),

            VisitMediaStatus.VisitNotInProgress => ApiResponseFactory.BadRequest(
                this,
                "visit_not_in_progress",
                "Images can only be uploaded while the visit is in progress."),

            VisitMediaStatus.ImageFileRequired => ApiResponseFactory.BadRequest(
                this,
                "image_file_required",
                "Image file is required."),

            VisitMediaStatus.InvalidImageType => ApiResponseFactory.BadRequest(
                this,
                "invalid_image_type",
                "Only JPEG, PNG, and WEBP images are allowed."),

            VisitMediaStatus.ImageSizeExceeded => ApiResponseFactory.BadRequest(
                this,
                "image_size_exceeded",
                "Image size exceeds the 5 MB limit."),

            VisitMediaStatus.MaxVisitImagesReached => ApiResponseFactory.BadRequest(
                this,
                "max_visit_images_reached",
                "Maximum number of images for this visit has been reached."),

            _ => Problem("Unexpected visit media workflow result.")
        };
    }

    private ActionResult<IReadOnlyList<VisitImageResponseDto>> MapMediaListResult(
        VisitMediaResult<List<VisitImageResponseDto>> result)
    {
        return result.Status switch
        {
            VisitMediaStatus.Success => Ok((IReadOnlyList<VisitImageResponseDto>)(result.Data ?? new List<VisitImageResponseDto>())),

            VisitMediaStatus.VisitNotFound => ApiResponseFactory.NotFound(
                this,
                "visit_not_found",
                "Visit was not found."),

            VisitMediaStatus.Forbidden => Forbid(),

            _ => Problem("Unexpected visit media query result.")
        };
    }

    private IActionResult MapDeleteMediaResult(VisitMediaResult<bool> result)
    {
        return result.Status switch
        {
            VisitMediaStatus.Success => NoContent(),

            VisitMediaStatus.VisitImageNotFound => ApiResponseFactory.NotFound(
                this,
                "visit_image_not_found",
                "Visit image was not found."),

            VisitMediaStatus.Forbidden => Forbid(),

            VisitMediaStatus.VisitNotInProgress => ApiResponseFactory.BadRequest(
                this,
                "visit_not_in_progress",
                "Visit images can only be deleted while the visit is in progress."),

            _ => Problem("Unexpected visit media delete result.")
        };
    }

    private IActionResult MapImageContentResult(VisitMediaResult<VisitImageContentPayload> result)
    {
        return result.Status switch
        {
            VisitMediaStatus.Success when result.Data is not null => BuildImageFileResult(result.Data),

            VisitMediaStatus.VisitImageNotFound => ApiResponseFactory.NotFound(
                this,
                "visit_image_not_found",
                "Visit image was not found."),

            VisitMediaStatus.Forbidden => Forbid(),

            _ => Problem("Unexpected visit media content result.")
        };
    }

    private IActionResult BuildImageFileResult(VisitImageContentPayload payload)
    {
        Response.Headers["Cache-Control"] = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return PhysicalFile(
            payload.PhysicalPath,
            payload.ContentType,
            fileDownloadName: null,
            enableRangeProcessing: true);
    }

    private static VisitResponseDto MapVisit(Mando.Api.Entities.Visit visit)
    {
        return new VisitResponseDto
        {
            Id = visit.Id,
            CustomerId = visit.CustomerId,
            CustomerName = visit.Customer.Name,
            SalesRepId = visit.SalesRepId,
            SalesRepName = visit.SalesRep.FullName,
            CheckInAtUtc = visit.CheckInAtUtc,
            CheckInLatitude = visit.CheckInLatitude,
            CheckInLongitude = visit.CheckInLongitude,
            CheckInAccuracyInMeters = visit.CheckInAccuracyInMeters,
            CheckOutAtUtc = visit.CheckOutAtUtc,
            CheckOutLatitude = visit.CheckOutLatitude,
            CheckOutLongitude = visit.CheckOutLongitude,
            CheckOutAccuracyInMeters = visit.CheckOutAccuracyInMeters,
            DistanceFromCustomerInMeters = visit.DistanceFromCustomerInMeters,
            Status = visit.Status,
            Outcome = visit.Outcome,
            Notes = visit.Notes,
            RowVersion = RowVersionTokenHelper.Encode(visit.RowVersion),
            CreatedAtUtc = visit.CreatedAtUtc,
            UpdatedAtUtc = visit.UpdatedAtUtc
        };
    }
}