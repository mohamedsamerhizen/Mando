using Mando.Api.Enums;

namespace Mando.Api.DTOs.Operations;

public class OperationsAlertReviewDto
{
    public Guid Id { get; set; }
    public string AlertKey { get; set; } = string.Empty;
    public string AlertFingerprint { get; set; } = string.Empty;
    public OperationsAlertCategory Category { get; set; }
    public OperationsAlertEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public DateTime TriggeredAtUtc { get; set; }
    public OperationsAlertReviewStatus Status { get; set; }
    public string? Comment { get; set; }
    public Guid ReviewedByUserId { get; set; }
    public string ReviewedByUserFullName { get; set; } = string.Empty;
    public DateTime ReviewedAtUtc { get; set; }
}