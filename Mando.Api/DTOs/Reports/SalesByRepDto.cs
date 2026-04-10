namespace Mando.Api.DTOs.Reports;

public class SalesByRepDto
{
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;
    public int OrdersCount { get; set; }
    public decimal TotalSales { get; set; }
}