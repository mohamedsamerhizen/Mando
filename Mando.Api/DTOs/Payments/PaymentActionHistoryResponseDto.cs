using Mando.Api.Enums;

namespace Mando.Api.DTOs.Payments;

public class PaymentActionHistoryResponseDto
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }

    public PaymentActionType ActionType { get; set; }

    public PaymentStatus? PreviousStatus { get; set; }
    public PaymentStatus NewStatus { get; set; }

    public Guid PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;

    public decimal? BalanceBeforeAction { get; set; }
    public decimal? BalanceAfterAction { get; set; }

    public string? Comment { get; set; }

    public DateTime ActionAtUtc { get; set; }
}