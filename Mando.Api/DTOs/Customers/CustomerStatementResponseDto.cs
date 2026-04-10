namespace Mando.Api.DTOs.Customers;

public class CustomerStatementResponseDto
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

    public int OrdersCount { get; set; }
    public int PaymentsCount { get; set; }

    public List<CustomerStatementOrderDto> RecentOrders { get; set; } = [];
    public List<CustomerStatementPaymentDto> RecentPayments { get; set; } = [];
}