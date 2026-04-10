namespace Mando.Api.DTOs.Reports;

public class SalesRepPerformanceReportResponseDto
{
    public DateTime DateFromUtc { get; set; }
    public DateTime DateToUtc { get; set; }
    public Guid? SalesRepId { get; set; }

    public int TotalSalesReps { get; set; }

    public int TotalVisits { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSalesAmount { get; set; }

    public int TotalPayments { get; set; }
    public int TotalApprovedPayments { get; set; }
    public int TotalRejectedPayments { get; set; }
    public decimal TotalApprovedCollectionsAmount { get; set; }

    public List<SalesRepPerformanceReportItemDto> Items { get; set; } = [];
}