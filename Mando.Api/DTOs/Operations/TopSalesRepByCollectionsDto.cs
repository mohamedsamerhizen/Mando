namespace Mando.Api.DTOs.Operations;

public class TopSalesRepByCollectionsDto
{
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;
    public int ApprovedPaymentsCount { get; set; }
    public decimal ApprovedCollectionsAmount { get; set; }
}