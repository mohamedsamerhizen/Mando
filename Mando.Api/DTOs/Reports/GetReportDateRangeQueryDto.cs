namespace Mando.Api.DTOs.Reports;

public class GetReportDateRangeQueryDto
{
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }
}