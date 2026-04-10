namespace Mando.Api.DTOs.Payments;

public class PaymentPendingAgingBucketDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}