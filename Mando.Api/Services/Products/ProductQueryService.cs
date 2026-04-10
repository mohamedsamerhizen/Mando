
using Mando.Api.Data;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Products;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Products;
using Mando.Api.Models.Products;
using Microsoft.EntityFrameworkCore;

namespace Mando.Api.Services.Products;

public class ProductQueryService : IProductQueryService
{
    private readonly AppDbContext _context;

    public ProductQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductQueryResult<PagedResultDto<ProductResponseDto>>> GetAllAsync(GetProductsQueryDto query)
    {
        var productsQuery = _context.Products.AsQueryable();

        if (query.ActiveOnly == true)
        {
            productsQuery = productsQuery.Where(x => x.Status == ProductStatus.Active);
        }

        var normalizedSearch = QueryFilterNormalizationHelper.NormalizeUpperInvariant(query.Search);
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            productsQuery = productsQuery.Where(x =>
                x.Name.ToUpper().Contains(normalizedSearch) ||
                x.Code.ToUpper().Contains(normalizedSearch));
        }

        var result = await productsQuery
            .OrderBy(x => x.Name)
            .Select(x => new ProductResponseDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Description = x.Description,
                UnitPrice = x.UnitPrice,
                Status = x.Status,
                RowVersion = RowVersionTokenHelper.Encode(x.RowVersion),
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .AsNoTracking()
            .ToPagedResultAsync(query.PageNumber, query.PageSize);

        return new ProductQueryResult<PagedResultDto<ProductResponseDto>>
        {
            Status = ProductQueryStatus.Success,
            Data = result
        };
    }

    public async Task<ProductQueryResult<ProductResponseDto>> GetByIdAsync(Guid productId)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == productId);

        if (product is null)
        {
            return new ProductQueryResult<ProductResponseDto>
            {
                Status = ProductQueryStatus.ProductNotFound
            };
        }

        return new ProductQueryResult<ProductResponseDto>
        {
            Status = ProductQueryStatus.Success,
            Data = new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Code = product.Code,
                Description = product.Description,
                UnitPrice = product.UnitPrice,
                Status = product.Status,
                RowVersion = RowVersionTokenHelper.Encode(product.RowVersion),
                CreatedAtUtc = product.CreatedAtUtc,
                UpdatedAtUtc = product.UpdatedAtUtc
            }
        };
    }

    public async Task<ProductQueryResult<IReadOnlyList<ProductActionHistoryResponseDto>>> GetHistoryAsync(Guid productId)
    {
        var productExists = await _context.Products
            .AsNoTracking()
            .AnyAsync(x => x.Id == productId);

        if (!productExists)
        {
            return new ProductQueryResult<IReadOnlyList<ProductActionHistoryResponseDto>>
            {
                Status = ProductQueryStatus.ProductNotFound
            };
        }

        var history = await _context.ProductActionHistories
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.ActionAtUtc)
            .Select(x => new ProductActionHistoryResponseDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ActionType = x.ActionType,
                PreviousName = x.PreviousName,
                NewName = x.NewName,
                PreviousCode = x.PreviousCode,
                NewCode = x.NewCode,
                PreviousUnitPrice = x.PreviousUnitPrice,
                NewUnitPrice = x.NewUnitPrice,
                PreviousStatus = x.PreviousStatus,
                NewStatus = x.NewStatus,
                PerformedByUserId = x.PerformedByUserId,
                PerformedByUserName = x.PerformedByUserFullName,
                Comment = x.Comment,
                ActionAtUtc = x.ActionAtUtc
            })
            .AsNoTracking()
            .ToListAsync();

        return new ProductQueryResult<IReadOnlyList<ProductActionHistoryResponseDto>>
        {
            Status = ProductQueryStatus.Success,
            Data = history
        };
    }
}
