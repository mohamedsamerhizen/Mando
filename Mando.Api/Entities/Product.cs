using Mando.Api.Common;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class Product : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal UnitPrice { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Active;

    public ICollection<ProductActionHistory> ActionHistories { get; set; } = new List<ProductActionHistory>();
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}