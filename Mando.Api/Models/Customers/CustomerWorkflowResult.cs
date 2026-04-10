using Mando.Api.Entities;
using Mando.Api.Enums;

namespace Mando.Api.Models.Customers;

public sealed class CustomerWorkflowResult
{
    public CustomerWorkflowStatus Status { get; init; }
    public Customer? Customer { get; init; }
    public string? AssignedSalesRepName { get; init; }
}