namespace Mando.Api.DTOs.Orders;

public class OrderActiveAgingBucketDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}