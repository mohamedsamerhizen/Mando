using Mando.Api.Entities;
using Mando.Api.Enums;

namespace Mando.Api.Models.Orders;

public sealed class CreateOrderResult
{
    public OrderWorkflowStatus Status { get; init; }
    public Order? Order { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal ProjectedBalance { get; init; }
    public decimal CreditLimit { get; init; }
}