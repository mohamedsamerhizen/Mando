using Mando.Api.DTOs.Products;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Products;

namespace Mando.Api.Interfaces.Products;

public interface IProductWorkflowService
{
    Task<ProductWorkflowResult> CreateAsync(CreateProductRequestDto request, AppUser currentUser);

    Task<ProductWorkflowResult> UpdateAsync(Guid productId, UpdateProductRequestDto request, AppUser currentUser);

    Task<ProductWorkflowResult> ChangeStatusAsync(Guid productId, ChangeProductStatusRequestDto request, AppUser currentUser);
}