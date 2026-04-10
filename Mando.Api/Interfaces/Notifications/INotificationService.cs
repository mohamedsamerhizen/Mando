using Mando.Api.Enums;

namespace Mando.Api.Interfaces.Notifications;

public interface INotificationService
{
    Task CreateForUserAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid? paymentId = null);

    Task CreateForRolesAsync(
        string[] roles,
        NotificationType type,
        string title,
        string message,
        Guid? paymentId = null);
}
