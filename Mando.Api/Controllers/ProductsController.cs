
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.Common;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Products;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Products;
using Mando.Api.Models.Products;
using Mando.Api.Helpers;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : CurrentUserAwareControllerBase
{
    private readonly IProductWorkflowService _productWorkflowService;
    private readonly IProductQueryService _productQueryService;

    public ProductsController(
        ICurrentUserContext currentUserContext,
        IProductWorkflowService productWorkflowService,
        IProductQueryService productQueryService)
        : base(currentUserContext)
    {
        _productWorkflowService = productWorkflowService;
        _productQueryService = productQueryService;
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<ProductResponseDto>> Create(CreateProductRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _productWorkflowService.CreateAsync(request, currentUser);

        return result.Status switch
        {
            ProductWorkflowStatus.Success => CreatedAtAction(
                nameof(GetById),
                new { id = result.Product!.Id },
                MapProduct(result.Product!)),

            ProductWorkflowStatus.ProductNameRequired => ApiResponseFactory.BadRequest(
                this,
                "product_name_required",
                "Product name is required."),

            ProductWorkflowStatus.ProductCodeRequired => ApiResponseFactory.BadRequest(
                this,
                "product_code_required",
                "Product code is required."),

            ProductWorkflowStatus.ProductCodeAlreadyExists => ApiResponseFactory.BadRequest(
                this,
                "product_code_already_exists",
                "Product code already exists."),

            _ => Problem("Unexpected product create workflow result.")
        };
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ProductResponseDto>>> GetAll([FromQuery] GetProductsQueryDto query)
    {
        var result = await _productQueryService.GetAllAsync(query);
        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponseDto>> GetById(Guid id)
    {
        var result = await _productQueryService.GetByIdAsync(id);
        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<ProductActionHistoryResponseDto>>> GetHistory(Guid id)
    {
        var result = await _productQueryService.GetHistoryAsync(id);
        return MapQueryResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<ProductResponseDto>> Update(Guid id, UpdateProductRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _productWorkflowService.UpdateAsync(id, request, currentUser);

        return result.Status switch
        {
            ProductWorkflowStatus.Success => Ok(MapProduct(result.Product!)),

            ProductWorkflowStatus.ProductNotFound => ApiResponseFactory.NotFound(
                this,
                "product_not_found",
                "Product was not found."),

            ProductWorkflowStatus.ProductNameRequired => ApiResponseFactory.BadRequest(
                this,
                "product_name_required",
                "Product name is required."),

            ProductWorkflowStatus.ProductCodeRequired => ApiResponseFactory.BadRequest(
                this,
                "product_code_required",
                "Product code is required."),

            ProductWorkflowStatus.ProductCodeAlreadyExists => ApiResponseFactory.BadRequest(
                this,
                "product_code_already_exists",
                "Product code already exists."),

            ProductWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is invalid."),

            ProductWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "concurrency_conflict",
                "The product was modified by another user. Refresh and try again."),

            _ => Problem("Unexpected product update workflow result.")
        };
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<ProductResponseDto>> ChangeStatus(Guid id, ChangeProductStatusRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _productWorkflowService.ChangeStatusAsync(id, request, currentUser);

        return result.Status switch
        {
            ProductWorkflowStatus.Success => Ok(MapProduct(result.Product!)),

            ProductWorkflowStatus.ProductNotFound => ApiResponseFactory.NotFound(
                this,
                "product_not_found",
                "Product was not found."),

            ProductWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is invalid."),

            ProductWorkflowStatus.ProductStatusUnchanged => ApiResponseFactory.BadRequest(
                this,
                "product_status_unchanged",
                "Product status is already set to the requested value."),

            ProductWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "concurrency_conflict",
                "The product was modified by another user. Refresh and try again."),

            _ => Problem("Unexpected product status workflow result.")
        };
    }

    private ActionResult<T> MapQueryResult<T>(ProductQueryResult<T> result)
    {
        switch (result.Status)
        {
            case ProductQueryStatus.Success:
                return Ok(result.Data);

            case ProductQueryStatus.ProductNotFound:
                return new ActionResult<T>(ApiResponseFactory.NotFound(
                    this,
                    "product_not_found",
                    "Product was not found."));

            default:
                return new ActionResult<T>(Problem("Unexpected product query result."));
        }
    }

    private static ProductResponseDto MapProduct(Mando.Api.Entities.Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Code = product.Code,
            Description = product.Description,
            UnitPrice = product.UnitPrice,
            Status = product.Status,
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc
        };
    }
}


