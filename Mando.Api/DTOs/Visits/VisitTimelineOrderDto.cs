using Mando.Api.Enums;

namespace Mando.Api.DTOs.Visits;

public class VisitTimelineOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public PaymentType PaymentType { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}