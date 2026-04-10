using Mando.Api.Enums;

namespace Mando.Api.DTOs.Visits;

public class VisitResponseDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;

    public DateTime CheckInAtUtc { get; set; }
    public decimal CheckInLatitude { get; set; }
    public decimal CheckInLongitude { get; set; }
    public decimal CheckInAccuracyInMeters { get; set; }

    public DateTime? CheckOutAtUtc { get; set; }
    public decimal? CheckOutLatitude { get; set; }
    public decimal? CheckOutLongitude { get; set; }
    public decimal? CheckOutAccuracyInMeters { get; set; }

    public double DistanceFromCustomerInMeters { get; set; }

    public VisitStatus Status { get; set; }
    public VisitOutcome Outcome { get; set; }

    public string? Notes { get; set; }

    public string RowVersion { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}