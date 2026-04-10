using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class OrderActionHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public OrderActionType ActionType { get; set; }

    public OrderStatus? PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }

    public Guid PerformedByUserId { get; set; }
    public AppUser PerformedByUser { get; set; } = default!;

    public string PerformedByUserFullName { get; set; } = string.Empty;

    public decimal? BalanceBeforeAction { get; set; }
    public decimal? BalanceAfterAction { get; set; }

    public string? Comment { get; set; }

    public DateTime ActionAtUtc { get; set; } = DateTime.UtcNow;
}