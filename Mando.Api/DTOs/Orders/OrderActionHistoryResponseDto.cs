using Mando.Api.Enums;

namespace Mando.Api.DTOs.Orders;

public class OrderActionHistoryResponseDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }

    public OrderActionType ActionType { get; set; }

    public OrderStatus? PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }

    public Guid PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;

    public decimal? BalanceBeforeAction { get; set; }
    public decimal? BalanceAfterAction { get; set; }

    public string? Comment { get; set; }

    public DateTime ActionAtUtc { get; set; }
}