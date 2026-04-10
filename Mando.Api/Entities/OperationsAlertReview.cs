using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class OperationsAlertReview : AuditableEntity
{
    public string AlertKey { get; set; } = string.Empty;
    public string AlertFingerprint { get; set; } = string.Empty;

    public OperationsAlertCategory Category { get; set; }
    public OperationsAlertEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public DateTime TriggeredAtUtc { get; set; }

    public string ShortReasonSnapshot { get; set; } = string.Empty;

    public OperationsAlertReviewStatus Status { get; set; }
    public string? Comment { get; set; }

    public Guid ReviewedByUserId { get; set; }
    public AppUser ReviewedByUser { get; set; } = default!;
    public string ReviewedByUserFullName { get; set; } = string.Empty;
    public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;
}