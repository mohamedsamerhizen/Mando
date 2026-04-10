namespace Mando.Api.DTOs.Reports;

public class CollectionsByRepDto
{
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;
    public int ApprovedPaymentsCount { get; set; }
    public decimal TotalCollections { get; set; }
}