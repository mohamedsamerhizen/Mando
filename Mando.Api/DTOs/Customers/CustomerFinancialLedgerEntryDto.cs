using Mando.Api.Enums;

namespace Mando.Api.DTOs.Customers;

public class CustomerFinancialLedgerEntryDto
{
    public DateTime OccurredAtUtc { get; set; }
    public CustomerFinancialLedgerEntryType EntryType { get; set; }

    public Guid? EntityId { get; set; }
    public Guid? VisitId { get; set; }

    public string? ReferenceNumber { get; set; }
    public string Description { get; set; } = string.Empty;

    public decimal DeltaAmount { get; set; }
    public decimal RunningBalance { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public OrderStatus? OrderStatus { get; set; }

    public Guid? ActorUserId { get; set; }
    public string? ActorUserName { get; set; }

    public string? Comment { get; set; }
}