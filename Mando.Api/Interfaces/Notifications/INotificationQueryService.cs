using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Notifications;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Notifications;

namespace Mando.Api.Interfaces.Notifications;

public interface INotificationQueryService
{
    Task<NotificationQueryResult<PagedResultDto<NotificationResponseDto>>> GetMyNotificationsAsync(
        GetNotificationsQueryDto query,
        AppUser currentUser);

    Task<NotificationQueryResult<NotificationResponseDto>> GetByIdAsync(
        Guid notificationId,
        AppUser currentUser);

    Task<NotificationQueryResult<NotificationUnreadSummaryResponseDto>> GetMyUnreadSummaryAsync(
        AppUser currentUser);
}
