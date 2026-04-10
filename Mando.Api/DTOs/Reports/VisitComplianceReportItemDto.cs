using Mando.Api.Enums;

namespace Mando.Api.DTOs.Reports;

public class VisitComplianceReportItemDto
{
    public Guid Id { get; set; }

    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal AccuracyInMeters { get; set; }
    public double DistanceFromCustomerInMeters { get; set; }

    public VisitComplianceStatus ComplianceStatus { get; set; }
    public bool IsSuccessful { get; set; }
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}