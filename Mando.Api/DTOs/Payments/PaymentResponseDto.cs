using Mando.Api.Enums;

namespace Mando.Api.DTOs.Payments;

public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;

    public Guid VisitId { get; set; }

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    public string? RejectionReason { get; set; }

    public bool IsPending => Status == PaymentStatus.Pending;
    public bool IsApproved => Status == PaymentStatus.Approved;
    public bool IsRejected => Status == PaymentStatus.Rejected;
    public bool IsReversed => Status == PaymentStatus.Reversed;
    public bool IsVoided => IsReversed;

    public string RowVersion { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
