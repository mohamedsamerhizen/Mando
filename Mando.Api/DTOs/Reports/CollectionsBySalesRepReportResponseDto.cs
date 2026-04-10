namespace Mando.Api.DTOs.Reports;

public class CollectionsBySalesRepReportResponseDto
{
    public DateTime DateFromUtc { get; set; }
    public DateTime DateToUtc { get; set; }
    public Guid? SalesRepId { get; set; }

    public int TotalSalesReps { get; set; }

    public int TotalPaymentsCount { get; set; }
    public int TotalPendingPaymentsCount { get; set; }
    public int TotalApprovedPaymentsCount { get; set; }
    public int TotalRejectedPaymentsCount { get; set; }

    public decimal TotalPaymentsAmount { get; set; }
    public decimal TotalApprovedPaymentsAmount { get; set; }
    public decimal TotalRejectedPaymentsAmount { get; set; }

    public List<CollectionsBySalesRepReportItemDto> Items { get; set; } = [];
}