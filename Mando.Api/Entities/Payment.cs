using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class Payment : AuditableEntity
{
    public string PaymentNumber { get; set; } = string.Empty;

    public Guid VisitId { get; set; }
    public Visit Visit { get; set; } = default!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public Guid SalesRepId { get; set; }
    public AppUser SalesRep { get; set; } = default!;

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? RejectionReason { get; set; }

    public ICollection<PaymentActionHistory> ActionHistories { get; set; } = new List<PaymentActionHistory>();
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
