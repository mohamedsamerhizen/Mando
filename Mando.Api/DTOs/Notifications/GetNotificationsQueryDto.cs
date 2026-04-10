using Mando.Api.DTOs.Common;

namespace Mando.Api.DTOs.Notifications;

public class GetNotificationsQueryDto : PagedQueryDto
{
    public bool? IsRead { get; set; }
}