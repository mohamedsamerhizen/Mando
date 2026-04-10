using Mando.Api.Entities;
using Mando.Api.Enums;

namespace Mando.Api.Models.Payments;

public sealed class PaymentWorkflowResult
{
    public PaymentWorkflowStatus Status { get; init; }
    public Payment? Payment { get; init; }
    public decimal CurrentBalance { get; init; }
}