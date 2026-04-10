using Mando.Api.Enums;

namespace Mando.Api.DTOs.Payments;

public class PaymentMethodBreakdownDto
{
    public PaymentMethod PaymentMethod { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}