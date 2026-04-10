namespace Mando.Api.DTOs.Operations;

public class UnifiedOperationsAttentionSummaryDto
{
    public int PendingPaymentsCount { get; set; }
    public int StalePendingPaymentsCount { get; set; }
    public int ApprovalBlockedPaymentsCount { get; set; }
    public int PaymentsRequiringAttentionCount { get; set; }

    public int ActiveOrdersCount { get; set; }
    public int StaleActiveOrdersCount { get; set; }

    public int InProgressVisitsCount { get; set; }
    public int StaleInProgressVisitsCount { get; set; }

    public int TotalAttentionSignals { get; set; }
}