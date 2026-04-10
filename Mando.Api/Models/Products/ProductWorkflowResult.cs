using Mando.Api.Entities;
using Mando.Api.Enums;

namespace Mando.Api.Models.Products;

public sealed class ProductWorkflowResult
{
    public ProductWorkflowStatus Status { get; init; }
    public Product? Product { get; init; }
}