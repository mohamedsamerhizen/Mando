using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class PaymentActionHistory : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = default!;

    public PaymentActionType ActionType { get; set; }

    public PaymentStatus? PreviousStatus { get; set; }
    public PaymentStatus NewStatus { get; set; }

    public Guid PerformedByUserId { get; set; }
    public AppUser PerformedByUser { get; set; } = default!;

    public string PerformedByUserFullName { get; set; } = string.Empty;

    public decimal? BalanceBeforeAction { get; set; }
    public decimal? BalanceAfterAction { get; set; }

    public string? Comment { get; set; }

    public DateTime ActionAtUtc { get; set; } = DateTime.UtcNow;
}