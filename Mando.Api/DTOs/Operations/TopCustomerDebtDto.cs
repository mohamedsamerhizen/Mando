namespace Mando.Api.DTOs.Operations;

public class TopCustomerDebtDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal TotalOrders { get; set; }
    public decimal ApprovedPayments { get; set; }
    public decimal CurrentBalance { get; set; }
}