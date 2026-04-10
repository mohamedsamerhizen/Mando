using Mando.Api.Enums;

namespace Mando.Api.DTOs.Visits;

public class VisitTimelineResponseDto
{
    public Guid VisitId { get; set; }

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;

    public DateTime CheckInAtUtc { get; set; }
    public decimal CheckInLatitude { get; set; }
    public decimal CheckInLongitude { get; set; }
    public decimal? CheckInAccuracyInMeters { get; set; }

    public DateTime? CheckOutAtUtc { get; set; }
    public decimal? CheckOutLatitude { get; set; }
    public decimal? CheckOutLongitude { get; set; }
    public decimal? CheckOutAccuracyInMeters { get; set; }

    public double DistanceFromCustomerInMeters { get; set; }
    public VisitStatus Status { get; set; }
    public VisitOutcome Outcome { get; set; }
    public string? Notes { get; set; }

    public int ImagesCount { get; set; }
    public int OrdersCount { get; set; }
    public int PaymentsCount { get; set; }

    public List<VisitTimelineImageDto> Images { get; set; } = [];
    public List<VisitTimelineOrderDto> Orders { get; set; } = [];
    public List<VisitTimelinePaymentDto> Payments { get; set; } = [];
}