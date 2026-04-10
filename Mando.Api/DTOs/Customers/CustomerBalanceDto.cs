namespace Mando.Api.DTOs.Customers;

public class CustomerBalanceDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }
    public decimal TotalOrders { get; set; }
    public decimal ApprovedPayments { get; set; }
    public decimal CurrentBalance { get; set; }
}