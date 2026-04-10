namespace Mando.Api.DTOs.Payments;

public class PaymentOperationsBacklogSnapshotDto
{
    public int PendingCount { get; set; }
    public decimal PendingAmount { get; set; }

    public int CustomersWithPendingPaymentsCount { get; set; }

    public int StalePendingCount { get; set; }
    public decimal StalePendingAmount { get; set; }

    public int ApprovalBlockedCount { get; set; }
    public int AttentionRequiredCount { get; set; }

    public int PendingNonCashWithoutReferenceCount { get; set; }
    public int PendingDuplicateReferenceCount { get; set; }

    public double? AveragePendingAgeInHours { get; set; }
    public double? OldestPendingAgeInHours { get; set; }
}