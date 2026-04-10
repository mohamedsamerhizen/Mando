namespace Mando.Api.DTOs.Operations;

public class TopSalesRepByVisitsDto
{
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;
    public int VisitsCount { get; set; }
    public int CompletedVisitsCount { get; set; }
}