namespace Mando.Api.DTOs.Operations;

public class UnifiedOperationsFlowSummaryDto
{
    public int StartedVisitsCount { get; set; }
    public int CompletedVisitsCount { get; set; }
    public int CancelledVisitsCount { get; set; }

    public int SubmittedOrdersCount { get; set; }
    public int CancelledOrdersCount { get; set; }

    public int SubmittedPaymentsCount { get; set; }
    public int ApprovedPaymentsCount { get; set; }
    public int RejectedPaymentsCount { get; set; }
}