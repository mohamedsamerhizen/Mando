using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class AuditLog : AuditableEntity
{
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }

    public string? UserFullName { get; set; }
    public string? UserEmail { get; set; }

    public AuditActionType ActionType { get; set; }

    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    public string Description { get; set; } = string.Empty;
}