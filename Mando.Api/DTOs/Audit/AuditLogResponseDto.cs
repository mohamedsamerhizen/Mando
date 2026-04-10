using Mando.Api.Enums;

namespace Mando.Api.DTOs.Audit;

public class AuditLogResponseDto
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }
    public string? UserFullName { get; set; }
    public string? UserEmail { get; set; }

    public AuditActionType ActionType { get; set; }

    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}