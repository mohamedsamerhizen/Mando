namespace Mando.Api.DTOs.Orders;

public class OrderSalesRepPerformanceDto
{
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;

    public int SubmittedCount { get; set; }
    public decimal SubmittedAmount { get; set; }

    public int CancelledCount { get; set; }
    public decimal CancelledAmount { get; set; }

    public int ActiveOrdersCount { get; set; }
    public decimal ActiveOrdersAmount { get; set; }
}