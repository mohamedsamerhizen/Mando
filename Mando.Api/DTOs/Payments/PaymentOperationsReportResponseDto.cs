namespace Mando.Api.DTOs.Payments;

public class PaymentOperationsReportResponseDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public DateTime RangeFromUtc { get; set; }
    public DateTime RangeToUtc { get; set; }

    public int StaleAfterHours { get; set; }

    public PaymentOperationsBacklogSnapshotDto BacklogSnapshot { get; set; } = new();
    public PaymentOperationsThroughputSummaryDto ThroughputSummary { get; set; } = new();

    public List<PaymentPendingAgingBucketDto> PendingAgingBuckets { get; set; } = [];
    public List<PaymentMethodBreakdownDto> SubmissionMethodBreakdown { get; set; } = [];
    public List<PaymentRejectionCategoryBreakdownDto> RejectionCategoryBreakdown { get; set; } = [];
    public List<PaymentReviewerPerformanceDto> ReviewerPerformance { get; set; } = [];
}