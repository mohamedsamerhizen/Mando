using Mando.Api.Enums;
using Mando.Api.Interfaces.Audit;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Notifications;

namespace Mando.Api.Services.Common;

public class WorkflowSideEffectService : IWorkflowSideEffectService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<WorkflowSideEffectService> _logger;

    public WorkflowSideEffectService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<WorkflowSideEffectService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task WriteAuditAsync(
        Guid? userId,
        AuditActionType actionType,
        string entityName,
        Guid entityId,
        string description)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

            await auditService.CreateAsync(
                userId,
                actionType,
                entityName,
                entityId,
                description);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Workflow audit side effect failed. ActionType: {ActionType} | Entity: {EntityName} | EntityId: {EntityId}",
                actionType,
                entityName,
                entityId);
            _logger.LogWarning("Audit side effect failed after the core workflow committed. Manual follow-up may be required for EntityId {EntityId}.", entityId);
        }
    }

    public async Task CreateNotificationForUserAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid? paymentId = null)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            await notificationService.CreateForUserAsync(
                userId,
                type,
                title,
                message,
                paymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Workflow user notification side effect failed. UserId: {UserId} | NotificationType: {NotificationType} | PaymentId: {PaymentId}",
                userId,
                type,
                paymentId);
            _logger.LogWarning("User notification side effect failed after the core workflow committed. Manual follow-up may be required for UserId {UserId}.", userId);
        }
    }

    public async Task CreateNotificationForRolesAsync(
        string[] roles,
        NotificationType type,
        string title,
        string message,
        Guid? paymentId = null)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            await notificationService.CreateForRolesAsync(
                roles,
                type,
                title,
                message,
                paymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Workflow role notification side effect failed. NotificationType: {NotificationType} | Roles: {Roles} | PaymentId: {PaymentId}",
                type,
                string.Join(", ", roles ?? Array.Empty<string>()),
                paymentId);
            _logger.LogWarning("Role notification side effect failed after the core workflow committed. Manual follow-up may be required for PaymentId {PaymentId}.", paymentId);
        }
    }
}
