using Mando.Api.Enums;

namespace Mando.Api.DTOs.Operations;

public class OperationsAlertItemDto
{
    public string AlertKey { get; set; } = string.Empty;
    public string AlertFingerprint { get; set; } = string.Empty;
    public OperationsAlertSeverity Severity { get; set; }
    public OperationsAlertCategory Category { get; set; }
    public OperationsAlertEntityType EntityType { get; set; }

    public Guid EntityId { get; set; }
    public string? EntityNumber { get; set; }

    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }

    public Guid? SalesRepId { get; set; }
    public string? SalesRepName { get; set; }

    public Guid? VisitId { get; set; }

    public string ShortReason { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;

    public DateTime TriggeredAtUtc { get; set; }
    public double AgeInHours { get; set; }

    public decimal? Amount { get; set; }
    public decimal? CurrentBalance { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? BalanceRatio { get; set; }

    public string? Reference { get; set; }
    public int? RelatedCount { get; set; }

    public OperationsAlertReviewStatus ReviewStatus { get; set; } = OperationsAlertReviewStatus.Open;
    public OperationsAlertReviewDto? LatestReview { get; set; }
}