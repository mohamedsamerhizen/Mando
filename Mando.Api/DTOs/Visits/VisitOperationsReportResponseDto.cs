namespace Mando.Api.DTOs.Visits;

public class VisitOperationsReportResponseDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public DateTime RangeFromUtc { get; set; }
    public DateTime RangeToUtc { get; set; }

    public int StaleAfterHours { get; set; }

    public VisitOperationsActiveSnapshotDto ActiveSnapshot { get; set; } = new();
    public VisitOperationsThroughputSummaryDto ThroughputSummary { get; set; } = new();

    public List<VisitOutcomeBreakdownDto> OutcomeBreakdown { get; set; } = [];
    public List<VisitSalesRepPerformanceDto> SalesRepPerformance { get; set; } = [];
    public List<VisitActiveAgingBucketDto> ActiveVisitAgingBuckets { get; set; } = [];
}