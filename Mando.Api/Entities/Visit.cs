using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class Visit : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public Guid SalesRepId { get; set; }
    public AppUser SalesRep { get; set; } = default!;

    public DateTime CheckInAtUtc { get; set; }
    public decimal CheckInLatitude { get; set; }
    public decimal CheckInLongitude { get; set; }
    public decimal CheckInAccuracyInMeters { get; set; }

    public DateTime? CheckOutAtUtc { get; set; }
    public decimal? CheckOutLatitude { get; set; }
    public decimal? CheckOutLongitude { get; set; }
    public decimal? CheckOutAccuracyInMeters { get; set; }

    public double DistanceFromCustomerInMeters { get; set; }

    public VisitStatus Status { get; set; } = VisitStatus.InProgress;
    public VisitOutcome Outcome { get; set; } = VisitOutcome.Pending;

    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}