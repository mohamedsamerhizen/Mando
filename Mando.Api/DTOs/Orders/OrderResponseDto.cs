using Mando.Api.Enums;

namespace Mando.Api.DTOs.Orders;

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    public Guid VisitId { get; set; }

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;

    public PaymentType PaymentType { get; set; }
    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public Guid? CancelledByUserId { get; set; }
    public string? CancelledByUserName { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }

    public string RowVersion { get; set; } = string.Empty;

    public List<OrderItemResponseDto> Items { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}