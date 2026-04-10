namespace Mando.Api.DTOs.Orders;

public class OrderOperationsThroughputSummaryDto
{
    public int SubmittedCount { get; set; }
    public decimal SubmittedAmount { get; set; }

    public int CancelledCount { get; set; }
    public decimal CancelledAmount { get; set; }

    public double? CancellationRatePercent { get; set; }
    public double? AverageCancellationTurnaroundHours { get; set; }
}