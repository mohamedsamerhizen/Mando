namespace Mando.Api.DTOs.Reports;

public class CollectionsBySalesRepReportItemDto
{
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;

    public int TotalPaymentsCount { get; set; }
    public int PendingPaymentsCount { get; set; }
    public int ApprovedPaymentsCount { get; set; }
    public int RejectedPaymentsCount { get; set; }

    public decimal TotalPaymentsAmount { get; set; }
    public decimal ApprovedPaymentsAmount { get; set; }
    public decimal RejectedPaymentsAmount { get; set; }
}