using Mando.Api.Enums;

namespace Mando.Api.DTOs.Operations;

public class OperationOrderSummaryDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;
    public Guid VisitId { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentType PaymentType { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}