using Mando.Api.Enums;

namespace Mando.Api.Interfaces.Audit;

public interface IAuditService
{
    Task CreateAsync(
        Guid? userId,
        AuditActionType actionType,
        string entityName,
        Guid entityId,
        string description);
}