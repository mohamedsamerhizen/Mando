using Mando.Api.Enums;

namespace Mando.Api.DTOs.Orders;

public class OrderPaymentTypeBreakdownDto
{
    public PaymentType PaymentType { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}