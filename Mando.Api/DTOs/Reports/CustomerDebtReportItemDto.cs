namespace Mando.Api.DTOs.Reports;

public class CustomerDebtReportItemDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;

    public Guid AssignedSalesRepId { get; set; }
    public string AssignedSalesRepName { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }
    public decimal TotalOrders { get; set; }
    public decimal ApprovedPayments { get; set; }
    public decimal CurrentBalance { get; set; }

    public DateTime? LastOrderDateUtc { get; set; }
    public DateTime? LastPaymentDateUtc { get; set; }
}