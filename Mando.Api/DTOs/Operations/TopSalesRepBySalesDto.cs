namespace Mando.Api.DTOs.Operations;

public class TopSalesRepBySalesDto
{
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;
    public int OrdersCount { get; set; }
    public decimal TotalSalesAmount { get; set; }
}