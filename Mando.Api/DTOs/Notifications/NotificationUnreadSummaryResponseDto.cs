using Mando.Api.Enums;

namespace Mando.Api.DTOs.Notifications;

public sealed class NotificationUnreadSummaryResponseDto
{
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public DateTime? LatestUnreadCreatedAtUtc { get; set; }
    public Dictionary<NotificationType, int> UnreadByType { get; set; } = new();
}
