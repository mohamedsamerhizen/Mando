using System.ComponentModel.DataAnnotations;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Reports;

public class GetVisitComplianceReportQueryDto
{
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }

    public Guid? SalesRepId { get; set; }
    public Guid? CustomerId { get; set; }

    [EnumDataType(typeof(VisitComplianceStatus))]
    public VisitComplianceStatus? ComplianceStatus { get; set; }

    public bool? IsSuccessful { get; set; }
}