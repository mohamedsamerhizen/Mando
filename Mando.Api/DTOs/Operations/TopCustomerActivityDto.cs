namespace Mando.Api.DTOs.Operations;

public class TopCustomerActivityDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int VisitsCount { get; set; }
    public int OrdersCount { get; set; }
    public int PaymentsCount { get; set; }
    public int ActivityScore { get; set; }
}