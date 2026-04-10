using Mando.Api.DTOs.Common;

namespace Mando.Api.DTOs.Products;

public class GetProductsQueryDto : PagedQueryDto
{
    public string? Search { get; set; }
    public bool? ActiveOnly { get; set; }
}