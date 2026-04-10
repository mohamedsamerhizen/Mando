using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class VisitAttemptLog : AuditableEntity
{
    public Guid SalesRepId { get; set; }
    public AppUser SalesRep { get; set; } = default!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal AccuracyInMeters { get; set; }

    public double DistanceFromCustomerInMeters { get; set; }

    public VisitComplianceStatus ComplianceStatus { get; set; }
    public bool IsSuccessful { get; set; }

    public string Reason { get; set; } = string.Empty;
}