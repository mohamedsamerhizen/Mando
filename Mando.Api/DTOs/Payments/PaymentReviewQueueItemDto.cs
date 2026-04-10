using Mando.Api.Enums;

namespace Mando.Api.DTOs.Payments;

public class PaymentReviewQueueItemDto
{
    public Guid PaymentId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;

    public Guid VisitId { get; set; }

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Reference { get; set; }

    public DateTime SubmittedAtUtc { get; set; }
    public double PendingForHours { get; set; }
    public bool IsStale { get; set; }

    public decimal CurrentOutstandingBalance { get; set; }
    public decimal? BalanceCoverageRatio { get; set; }

    public int PendingPaymentsForCustomerCount { get; set; }
    public int DuplicatePendingReferenceCount { get; set; }

    public List<PaymentReviewRiskFlag> ReviewRiskFlags { get; set; } = [];
    public bool NeedsReviewAttention => ReviewRiskFlags.Count > 0;
}