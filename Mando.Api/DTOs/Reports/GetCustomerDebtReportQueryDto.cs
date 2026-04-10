namespace Mando.Api.DTOs.Reports;

public class GetCustomerDebtReportQueryDto
{
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }
    public Guid? SalesRepId { get; set; }
    public bool PositiveBalanceOnly { get; set; } = true;
}