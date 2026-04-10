namespace Mando.Api.DTOs.Reports;

public class GetSalesRepPerformanceReportQueryDto
{
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }
    public Guid? SalesRepId { get; set; }
}