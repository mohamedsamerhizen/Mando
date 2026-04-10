using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Notifications;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Notifications;
using Mando.Api.Models.Notifications;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : CurrentUserAwareControllerBase
{
    private readonly INotificationQueryService _notificationQueryService;
    private readonly INotificationWorkflowService _notificationWorkflowService;

    public NotificationsController(
        ICurrentUserContext currentUserContext,
        INotificationQueryService notificationQueryService,
        INotificationWorkflowService notificationWorkflowService)
        : base(currentUserContext)
    {
        _notificationQueryService = notificationQueryService;
        _notificationWorkflowService = notificationWorkflowService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<NotificationResponseDto>>> GetMyNotifications([FromQuery] GetNotificationsQueryDto query)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _notificationQueryService.GetMyNotificationsAsync(query, currentUser);
        return MapQueryResult(result);
    }

    [HttpGet("unread-summary")]
    public async Task<ActionResult<NotificationUnreadSummaryResponseDto>> GetUnreadSummary()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _notificationQueryService.GetMyUnreadSummaryAsync(currentUser);
        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotificationResponseDto>> GetById(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _notificationQueryService.GetByIdAsync(id, currentUser);
        return MapQueryResult(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult<NotificationResponseDto>> MarkAsRead(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _notificationWorkflowService.MarkAsReadAsync(id, currentUser);
        return MapWorkflowResult(result);
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        await _notificationWorkflowService.MarkAllAsReadAsync(currentUser);
        return NoContent();
    }

    private ActionResult<T> MapQueryResult<T>(NotificationQueryResult<T> result)
    {
        return result.Status switch
        {
            Mando.Api.Enums.NotificationQueryStatus.Success => Ok(result.Data),
            Mando.Api.Enums.NotificationQueryStatus.NotificationNotFound => new ActionResult<T>(ApiResponseFactory.NotFound(
                this,
                "notification_not_found",
                "Notification was not found.")),
            _ => new ActionResult<T>(Problem("Unexpected notification query result."))
        };
    }

    private ActionResult<T> MapWorkflowResult<T>(NotificationWorkflowResult<T> result)
    {
        return result.Status switch
        {
            Mando.Api.Enums.NotificationWorkflowStatus.Success => Ok(result.Data),
            Mando.Api.Enums.NotificationWorkflowStatus.NotificationNotFound => new ActionResult<T>(ApiResponseFactory.NotFound(
                this,
                "notification_not_found",
                "Notification was not found.")),
            _ => new ActionResult<T>(Problem("Unexpected notification workflow result."))
        };
    }
}
