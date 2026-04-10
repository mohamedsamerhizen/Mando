using Mando.Api.DTOs.Notifications;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Notifications;

namespace Mando.Api.Interfaces.Notifications;

public interface INotificationWorkflowService
{
    Task<NotificationWorkflowResult<NotificationResponseDto>> MarkAsReadAsync(
        Guid notificationId,
        AppUser currentUser);

    Task<NotificationWorkflowResult<int>> MarkAllAsReadAsync(AppUser currentUser);
}