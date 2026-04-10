using Mando.Api.Enums;

namespace Mando.Api.Models.Products;

public sealed class ProductQueryResult<T>
{
    public ProductQueryStatus Status { get; init; }
    public T? Data { get; init; }
}