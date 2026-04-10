namespace Mando.Api.DTOs.Payments;

public class PaymentOperationsThroughputSummaryDto
{
    public int SubmittedCount { get; set; }
    public decimal SubmittedAmount { get; set; }

    public int ReviewedCount { get; set; }
    public decimal ReviewedAmount { get; set; }

    public int ApprovedCount { get; set; }
    public decimal ApprovedAmount { get; set; }

    public int RejectedCount { get; set; }
    public decimal RejectedAmount { get; set; }

    public double? ApprovalRatePercent { get; set; }
    public double? RejectionRatePercent { get; set; }

    public double? AverageApprovalTurnaroundHours { get; set; }
    public double? AverageRejectionTurnaroundHours { get; set; }
}