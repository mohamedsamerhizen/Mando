namespace Mando.Api.DTOs.Reports;

public class SalesRepPerformanceReportItemDto
{
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;

    public int TotalVisits { get; set; }
    public int CompletedVisits { get; set; }
    public int InProgressVisits { get; set; }
    public int CancelledVisits { get; set; }

    public int TotalOrders { get; set; }
    public decimal TotalSalesAmount { get; set; }

    public int TotalPayments { get; set; }
    public int ApprovedPaymentsCount { get; set; }
    public int RejectedPaymentsCount { get; set; }
    public decimal ApprovedCollectionsAmount { get; set; }
}