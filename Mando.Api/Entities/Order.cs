using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class Order : AuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public Guid VisitId { get; set; }
    public Visit Visit { get; set; } = default!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public Guid SalesRepId { get; set; }
    public AppUser SalesRep { get; set; } = default!;

    public PaymentType PaymentType { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Submitted;

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public Guid? CancelledByUserId { get; set; }
    public AppUser? CancelledByUser { get; set; }

    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public List<OrderItem> Items { get; set; } = [];
    public List<OrderActionHistory> ActionHistories { get; set; } = [];
}