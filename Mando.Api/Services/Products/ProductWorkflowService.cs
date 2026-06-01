
using Microsoft.EntityFrameworkCore;
using Mando.Api.Data;
using Mando.Api.DTOs.Products;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Products;
using Mando.Api.Models.Products;

namespace Mando.Api.Services.Products;

public class ProductWorkflowService : IProductWorkflowService
{
    private readonly AppDbContext _context;
    private readonly IWorkflowSideEffectService _workflowSideEffectService;

    public ProductWorkflowService(
        AppDbContext context,
        IWorkflowSideEffectService workflowSideEffectService)
    {
        _context = context;
        _workflowSideEffectService = workflowSideEffectService;
    }

    public async Task<ProductWorkflowResult> CreateAsync(CreateProductRequestDto request, AppUser currentUser)
    {
        var normalizedName = InputNormalizationHelper.NormalizeRequiredSingleLine(request.Name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.ProductNameRequired
            };
        }

        var normalizedCode = InputNormalizationHelper.NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.ProductCodeRequired
            };
        }

        var codeExists = await _context.Products.AnyAsync(x => x.Code == normalizedCode);
        if (codeExists)
        {
            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.ProductCodeAlreadyExists
            };
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Code = normalizedCode,
            Description = InputNormalizationHelper.NormalizeOptionalMultiline(request.Description),
            UnitPrice = request.UnitPrice,
            Status = ProductStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Products.Add(product);

            _context.ProductActionHistories.Add(CreateHistoryEntry(
                productId: product.Id,
                actionType: ProductActionType.Created,
                previousName: null,
                newName: product.Name,
                previousCode: null,
                newCode: product.Code,
                previousUnitPrice: null,
                newUnitPrice: product.UnitPrice,
                previousStatus: null,
                newStatus: product.Status,
                performedByUser: currentUser,
                comment: "Product created."));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.ProductCreated,
                nameof(Product),
                product.Id,
                $"Product '{product.Name}' with code '{product.Code}' was created by '{currentUser.FullName}'.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return new ProductWorkflowResult
        {
            Status = ProductWorkflowStatus.Success,
            Product = product
        };
    }

    public async Task<ProductWorkflowResult> UpdateAsync(Guid productId, UpdateProductRequestDto request, AppUser currentUser)
    {
        var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == productId);
        if (product is null)
        {
            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.ProductNotFound
            };
        }

        if (!RowVersionTokenHelper.TryDecode(request.RowVersion, out var originalRowVersion))
        {
            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.InvalidConcurrencyToken
            };
        }

        var normalizedName = InputNormalizationHelper.NormalizeRequiredSingleLine(request.Name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.ProductNameRequired
            };
        }

        var normalizedCode = InputNormalizationHelper.NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.ProductCodeRequired
            };
        }

        var codeExists = await _context.Products.AnyAsync(x => x.Code == normalizedCode && x.Id != productId);
        if (codeExists)
        {
            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.ProductCodeAlreadyExists
            };
        }

        _context.Entry(product).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

        var oldName = product.Name;
        var oldCode = product.Code;
        var oldUnitPrice = product.UnitPrice;
        var oldStatus = product.Status;

        product.Name = normalizedName;
        product.Code = normalizedCode;
        product.Description = InputNormalizationHelper.NormalizeOptionalMultiline(request.Description);
        product.UnitPrice = request.UnitPrice;
        product.UpdatedAtUtc = DateTime.UtcNow;

        var comment = oldUnitPrice != product.UnitPrice
            ? $"Product updated. Unit price changed from {oldUnitPrice:0.00} to {product.UnitPrice:0.00}."
            : "Product profile updated.";

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.ProductActionHistories.Add(CreateHistoryEntry(
                productId: product.Id,
                actionType: ProductActionType.Updated,
                previousName: oldName,
                newName: product.Name,
                previousCode: oldCode,
                newCode: product.Code,
                previousUnitPrice: oldUnitPrice,
                newUnitPrice: product.UnitPrice,
                previousStatus: oldStatus,
                newStatus: product.Status,
                performedByUser: currentUser,
                comment: comment));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.ProductUpdated,
                nameof(Product),
                product.Id,
                $"Product '{product.Name}' with code '{product.Code}' was updated by '{currentUser.FullName}'.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.ConcurrencyConflict
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return new ProductWorkflowResult
        {
            Status = ProductWorkflowStatus.Success,
            Product = product
        };
    }

    public async Task<ProductWorkflowResult> ChangeStatusAsync(Guid productId, ChangeProductStatusRequestDto request, AppUser currentUser)
    {
        var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == productId);
        if (product is null)
        {
            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.ProductNotFound
            };
        }

        if (!RowVersionTokenHelper.TryDecode(request.RowVersion, out var originalRowVersion))
        {
            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.InvalidConcurrencyToken
            };
        }

        if (product.Status == request.Status)
        {
            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.ProductStatusUnchanged,
                Product = product
            };
        }

        _context.Entry(product).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

        var oldStatus = product.Status;

        product.Status = request.Status;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.ProductActionHistories.Add(CreateHistoryEntry(
                productId: product.Id,
                actionType: ProductActionType.StatusChanged,
                previousName: product.Name,
                newName: product.Name,
                previousCode: product.Code,
                newCode: product.Code,
                previousUnitPrice: product.UnitPrice,
                newUnitPrice: product.UnitPrice,
                previousStatus: oldStatus,
                newStatus: product.Status,
                performedByUser: currentUser,
                comment: $"Product status changed from '{oldStatus}' to '{product.Status}'."));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.ProductStatusChanged,
                nameof(Product),
                product.Id,
                $"Product '{product.Name}' status changed from '{oldStatus}' to '{product.Status}' by '{currentUser.FullName}'.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            return new ProductWorkflowResult
            {
                Status = ProductWorkflowStatus.ConcurrencyConflict
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return new ProductWorkflowResult
        {
            Status = ProductWorkflowStatus.Success,
            Product = product
        };
    }

    private ProductActionHistory CreateHistoryEntry(
        Guid productId,
        ProductActionType actionType,
        string? previousName,
        string newName,
        string? previousCode,
        string newCode,
        decimal? previousUnitPrice,
        decimal newUnitPrice,
        ProductStatus? previousStatus,
        ProductStatus newStatus,
        AppUser performedByUser,
        string? comment)
    {
        return new ProductActionHistory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ActionType = actionType,
            PreviousName = previousName,
            NewName = newName,
            PreviousCode = previousCode,
            NewCode = newCode,
            PreviousUnitPrice = previousUnitPrice,
            NewUnitPrice = newUnitPrice,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            PerformedByUserId = performedByUser.Id,
            PerformedByUserFullName = performedByUser.FullName,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            ActionAtUtc = DateTime.UtcNow
        };
    }
}


