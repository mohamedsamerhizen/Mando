namespace Mando.Api.DTOs.Visits;

public class VisitOperationsThroughputSummaryDto
{
    public int StartedCount { get; set; }

    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }

    public double? CompletionRatePercent { get; set; }
    public double? CancellationRatePercent { get; set; }

    public double? AverageCompletedVisitDurationHours { get; set; }
    public double? AverageCancelledVisitDurationHours { get; set; }

    public int VisitsWithOrdersCount { get; set; }
    public int VisitsWithPaymentsCount { get; set; }
}