namespace Mando.Api.DTOs.Operations;

public class GetOperationsKpiQueryDto
{
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }

    public int TopCount { get; set; } = 5;
}