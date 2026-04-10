namespace Mando.Api.DTOs.Payments;

public class PaymentReviewQueueSummaryDto
{
    public int TotalPendingCount { get; set; }
    public decimal TotalPendingAmount { get; set; }

    public int FreshPendingCount { get; set; }
    public int AgingPendingCount { get; set; }
    public int StalePendingCount { get; set; }
    public decimal StalePendingAmount { get; set; }

    public int CustomersWithPendingPaymentsCount { get; set; }
    public int SalesRepsWithPendingPaymentsCount { get; set; }

    public int AttentionRequiredCount { get; set; }
    public int ApprovalBlockedCount { get; set; }
    public int MissingReferenceForNonCashCount { get; set; }
    public int DuplicateReferencePendingCount { get; set; }
    public int MultiPendingCustomerPaymentCount { get; set; }

    public DateTime? OldestPendingSubmittedAtUtc { get; set; }
    public double? OldestPendingAgeInHours { get; set; }
}