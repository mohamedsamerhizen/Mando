using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class ProductActionHistory : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public ProductActionType ActionType { get; set; }

    public string? PreviousName { get; set; }
    public string NewName { get; set; } = string.Empty;

    public string? PreviousCode { get; set; }
    public string NewCode { get; set; } = string.Empty;

    public decimal? PreviousUnitPrice { get; set; }
    public decimal NewUnitPrice { get; set; }

    public ProductStatus? PreviousStatus { get; set; }
    public ProductStatus NewStatus { get; set; }

    public Guid PerformedByUserId { get; set; }
    public AppUser PerformedByUser { get; set; } = default!;

    public string PerformedByUserFullName { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public DateTime ActionAtUtc { get; set; } = DateTime.UtcNow;
}