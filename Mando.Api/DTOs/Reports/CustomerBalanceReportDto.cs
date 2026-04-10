namespace Mando.Api.DTOs.Reports;

public class CustomerBalanceReportDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string SalesRepName { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }
    public decimal TotalOrders { get; set; }
    public decimal ApprovedPayments { get; set; }
    public decimal CurrentBalance { get; set; }
}