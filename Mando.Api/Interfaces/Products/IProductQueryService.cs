using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Products;
using Mando.Api.Models.Products;

namespace Mando.Api.Interfaces.Products;

public interface IProductQueryService
{
    Task<ProductQueryResult<PagedResultDto<ProductResponseDto>>> GetAllAsync(GetProductsQueryDto query);

    Task<ProductQueryResult<ProductResponseDto>> GetByIdAsync(Guid productId);

    Task<ProductQueryResult<IReadOnlyList<ProductActionHistoryResponseDto>>> GetHistoryAsync(Guid productId);
}