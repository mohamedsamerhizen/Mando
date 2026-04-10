using Mando.Api.Enums;

namespace Mando.Api.Interfaces.Common;

public interface IWorkflowSideEffectService
{
    Task WriteAuditAsync(
        Guid? userId,
        AuditActionType actionType,
        string entityName,
        Guid entityId,
        string description);

    Task CreateNotificationForUserAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid? paymentId = null);

    Task CreateNotificationForRolesAsync(
        string[] roles,
        NotificationType type,
        string title,
        string message,
        Guid? paymentId = null);
}
