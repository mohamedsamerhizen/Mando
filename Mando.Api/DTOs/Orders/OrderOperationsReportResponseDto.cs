namespace Mando.Api.DTOs.Orders;

public class OrderOperationsReportResponseDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public DateTime RangeFromUtc { get; set; }
    public DateTime RangeToUtc { get; set; }

    public int StaleAfterHours { get; set; }

    public OrderOperationsActiveSnapshotDto ActiveSnapshot { get; set; } = new();
    public OrderOperationsThroughputSummaryDto ThroughputSummary { get; set; } = new();

    public List<OrderActiveAgingBucketDto> ActiveOrderAgingBuckets { get; set; } = [];
    public List<OrderPaymentTypeBreakdownDto> PaymentTypeBreakdown { get; set; } = [];
    public List<OrderSalesRepPerformanceDto> SalesRepPerformance { get; set; } = [];
}