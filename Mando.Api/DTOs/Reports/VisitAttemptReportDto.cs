using Mando.Api.Enums;

namespace Mando.Api.DTOs.Reports;

public class VisitAttemptReportDto
{
    public Guid VisitAttemptLogId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal AccuracyInMeters { get; set; }
    public double DistanceFromCustomerInMeters { get; set; }
    public VisitComplianceStatus ComplianceStatus { get; set; }
    public bool IsSuccessful { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}