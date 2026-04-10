using Mando.Api.Enums;

namespace Mando.Api.DTOs.Products;

public class ProductActionHistoryResponseDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

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
    public string PerformedByUserName { get; set; } = string.Empty;

    public string? Comment { get; set; }
    public DateTime ActionAtUtc { get; set; }
}