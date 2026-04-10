using Mando.Api.Enums;

namespace Mando.Api.DTOs.Reports;

public class VisitComplianceReportResponseDto
{
    public DateTime DateFromUtc { get; set; }
    public DateTime DateToUtc { get; set; }

    public Guid? SalesRepId { get; set; }
    public Guid? CustomerId { get; set; }
    public VisitComplianceStatus? ComplianceStatus { get; set; }
    public bool? IsSuccessful { get; set; }

    public int TotalAttempts { get; set; }
    public int SuccessfulAttempts { get; set; }
    public int FailedAttempts { get; set; }

    public int CompliantCount { get; set; }
    public int OutOfRangeCount { get; set; }
    public int WeakAccuracyCount { get; set; }

    public List<VisitComplianceReportItemDto> Items { get; set; } = [];
}