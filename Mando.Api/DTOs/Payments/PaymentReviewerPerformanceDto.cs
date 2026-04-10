namespace Mando.Api.DTOs.Payments;

public class PaymentReviewerPerformanceDto
{
    public Guid ReviewerUserId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;

    public int ReviewedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }

    public decimal ReviewedAmount { get; set; }

    public double? AverageDecisionTurnaroundHours { get; set; }
}