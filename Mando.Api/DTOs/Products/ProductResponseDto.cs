using Mando.Api.Enums;

namespace Mando.Api.DTOs.Products;

public class ProductResponseDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal UnitPrice { get; set; }
    public ProductStatus Status { get; set; }

    public string RowVersion { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}