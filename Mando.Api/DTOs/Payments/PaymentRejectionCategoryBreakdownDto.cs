using Mando.Api.Enums;

namespace Mando.Api.DTOs.Payments;

public class PaymentRejectionCategoryBreakdownDto
{
    public PaymentRejectionCategory Category { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}