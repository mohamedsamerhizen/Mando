using Mando.Api.Enums;

namespace Mando.Api.DTOs.Operations;

public class OperationPaymentSummaryDto
{
    public Guid PaymentId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;
    public Guid VisitId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; }
    public string? Reference { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public string? RejectionReason { get; set; }
}
