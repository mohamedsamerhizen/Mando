using System.Data;
using Microsoft.EntityFrameworkCore;
using Mando.Api.Common;
using Mando.Api.Data;
using Mando.Api.DTOs.Orders;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Interfaces.Orders;
using Mando.Api.Interfaces.Users;
using Mando.Api.Interfaces.Visits;
using Mando.Api.Models.Financials;
using Mando.Api.Models.Orders;

namespace Mando.Api.Services.Orders;

public class OrderWorkflowService : IOrderWorkflowService
{
    private const int DocumentNumberCollisionRetryLimit = 3;

    private readonly AppDbContext _context;
    private readonly IWorkflowSideEffectService _workflowSideEffectService;
    private readonly ICustomerBalanceService _customerBalanceService;
    private readonly ICustomerFinancialLockService _customerFinancialLockService;
    private readonly IDocumentNumberGenerator _documentNumberGenerator;
    private readonly IUserStatusLockService _userStatusLockService;
    private readonly IVisitLifecycleLockService _visitLifecycleLockService;

    public OrderWorkflowService(
        AppDbContext context,
        IWorkflowSideEffectService workflowSideEffectService,
        ICustomerBalanceService customerBalanceService,
        ICustomerFinancialLockService customerFinancialLockService,
        IDocumentNumberGenerator documentNumberGenerator,
        IUserStatusLockService userStatusLockService,
        IVisitLifecycleLockService visitLifecycleLockService)
    {
        _context = context;
        _workflowSideEffectService = workflowSideEffectService;
        _customerBalanceService = customerBalanceService;
        _customerFinancialLockService = customerFinancialLockService;
        _documentNumberGenerator = documentNumberGenerator;
        _userStatusLockService = userStatusLockService;
        _visitLifecycleLockService = visitLifecycleLockService;
    }

    public async Task<OrderWorkflowResult> CreateAsync(CreateOrderRequestDto request, AppUser currentUser)
    {
        if (request.Items is null || request.Items.Count == 0)
            return new OrderWorkflowResult { Status = OrderWorkflowStatus.OrderItemsRequired };

        var productIds = request.Items
            .Select(x => x.ProductId)
            .ToList();

        if (productIds.Count != productIds.Distinct().Count())
            return new OrderWorkflowResult { Status = OrderWorkflowStatus.DuplicateProductsNotAllowed };

        if (request.Items.Any(x => x.Quantity <= 0))
            return new OrderWorkflowResult { Status = OrderWorkflowStatus.InvalidQuantity };

        var distinctProductIds = productIds.Distinct().ToList();

        CreditLimitCheckResult? creditCheck = null;
        Order? createdOrderEntity = null;

        for (var attempt = 0; attempt < DocumentNumberCollisionRetryLimit; attempt++)
        {
            var now = DateTime.UtcNow;
            var orderNumber = await _documentNumberGenerator.GenerateOrderNumberAsync();

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

            try
            {
                var userLockAcquired = await _userStatusLockService.LockAsync(currentUser.Id);
                if (!userLockAcquired)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult { Status = OrderWorkflowStatus.Forbidden };
                }

                var lockedSalesRep = await _context.Users.FirstOrDefaultAsync(x => x.Id == currentUser.Id);
                if (lockedSalesRep is null || !lockedSalesRep.IsActive)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult { Status = OrderWorkflowStatus.Forbidden };
                }

                var visitLockAcquired = await _visitLifecycleLockService.LockAsync(request.VisitId);
                if (!visitLockAcquired)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult { Status = OrderWorkflowStatus.VisitNotFound };
                }

                var visit = await _context.Visits
                    .Include(x => x.Customer)
                    .FirstOrDefaultAsync(x => x.Id == request.VisitId);

                if (visit is null)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult { Status = OrderWorkflowStatus.VisitNotFound };
                }

                if (visit.SalesRepId != currentUser.Id)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult { Status = OrderWorkflowStatus.Forbidden };
                }

                var customerLockAcquired = await _customerFinancialLockService.LockAsync(visit.CustomerId);
                if (!customerLockAcquired)
                {
                    await transaction.RollbackAsync();

                    return new OrderWorkflowResult
                    {
                        Status = OrderWorkflowStatus.CustomerNotFound
                    };
                }

                visit = await _context.Visits
                    .Include(x => x.Customer)
                    .FirstOrDefaultAsync(x => x.Id == request.VisitId);

                if (visit is null)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult { Status = OrderWorkflowStatus.VisitNotFound };
                }

                if (visit.Status != VisitStatus.InProgress)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult { Status = OrderWorkflowStatus.VisitNotInProgress };
                }

                if (visit.Customer.Status != CustomerStatus.Active)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult { Status = OrderWorkflowStatus.CustomerInactive };
                }

                var products = await _context.Products
                    .Where(x => distinctProductIds.Contains(x.Id) && x.Status == ProductStatus.Active)
                    .ToListAsync();

                if (products.Count != distinctProductIds.Count)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult { Status = OrderWorkflowStatus.InvalidOrInactiveProducts };
                }

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    VisitId = visit.Id,
                    CustomerId = visit.CustomerId,
                    SalesRepId = currentUser.Id,
                    PaymentType = request.PaymentType,
                    Status = OrderStatus.Submitted,
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    CreatedAtUtc = now
                };

                foreach (var itemRequest in request.Items)
                {
                    var product = products.First(x => x.Id == itemRequest.ProductId);
                    var lineTotal = itemRequest.Quantity * product.UnitPrice;

                    order.Items.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = product.Id,
                        Quantity = itemRequest.Quantity,
                        UnitPrice = product.UnitPrice,
                        LineTotal = lineTotal
                    });
                }

                order.TotalAmount = order.Items.Sum(x => x.LineTotal);

                creditCheck = await _customerBalanceService.CheckCreditLimitAsync(
                    visit.CustomerId,
                    order.TotalAmount);

                if (!creditCheck.Allowed)
                {
                    await transaction.RollbackAsync();

                    return new OrderWorkflowResult
                    {
                        Status = OrderWorkflowStatus.CreditLimitExceeded,
                        CurrentBalance = creditCheck.CurrentBalance,
                        ProjectedBalance = creditCheck.ProjectedBalance,
                        CreditLimit = creditCheck.CreditLimit
                    };
                }

                _context.Orders.Add(order);

                _context.OrderActionHistories.Add(CreateHistoryEntry(
                    orderId: order.Id,
                    actionType: OrderActionType.Submitted,
                    previousStatus: null,
                    newStatus: OrderStatus.Submitted,
                    performedByUser: currentUser,
                    balanceBeforeAction: creditCheck.CurrentBalance,
                    balanceAfterAction: creditCheck.ProjectedBalance,
                    comment: $"Order submitted with {order.Items.Count} item(s). Payment type: {order.PaymentType}."));

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                await _workflowSideEffectService.WriteAuditAsync(
                    currentUser.Id,
                    AuditActionType.OrderCreated,
                    nameof(Order),
                    order.Id,
                    $"Order '{order.OrderNumber}' was created for customer '{visit.Customer.Name}' by sales rep '{currentUser.FullName}' with total amount {order.TotalAmount:0.00}. Balance before order: {creditCheck.CurrentBalance:0.00}. Balance after order: {creditCheck.ProjectedBalance:0.00}.");
                createdOrderEntity = order;
                break;
            }
            catch (DbUpdateException ex) when (DbUpdateExceptionHelper.IsUniqueConstraintViolation(ex))
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                if (attempt == DocumentNumberCollisionRetryLimit - 1)
                    throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        if (createdOrderEntity is null)
            throw new InvalidOperationException("Failed to create order after retrying document number collisions.");

        var createdOrder = await LoadOrderAsync(createdOrderEntity.Id);

        return new OrderWorkflowResult
        {
            Status = OrderWorkflowStatus.Success,
            Order = createdOrder,
            CurrentBalance = creditCheck!.CurrentBalance,
            ProjectedBalance = creditCheck.ProjectedBalance,
            CreditLimit = creditCheck.CreditLimit
        };
    }

    public async Task<OrderWorkflowResult> CancelAsync(
        Guid orderId,
        CancelOrderRequestDto request,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var normalizedReason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
            return new OrderWorkflowResult { Status = OrderWorkflowStatus.CancellationReasonRequired };

        var order = await LoadOrderAsync(orderId);
        if (order is null)
            return new OrderWorkflowResult { Status = OrderWorkflowStatus.OrderNotFound };

        if (!RowVersionTokenHelper.TryDecode(request.RowVersion, out var originalRowVersion))
            return new OrderWorkflowResult { Status = OrderWorkflowStatus.InvalidConcurrencyToken };

        var isAdminOrManager =
            currentUserRoles.Contains(AppRoles.Admin) ||
            currentUserRoles.Contains(AppRoles.Manager);

        if (!isAdminOrManager && order.SalesRepId != currentUser.Id)
            return new OrderWorkflowResult { Status = OrderWorkflowStatus.Forbidden, Order = order };


        if (order.Status == OrderStatus.Cancelled)
        {
            return new OrderWorkflowResult
            {
                Status = OrderWorkflowStatus.OrderAlreadyCancelled,
                Order = order
            };
        }

        _context.Entry(order).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

        var previousStatus = order.Status;
        var decisionTimeUtc = DateTime.UtcNow;
        CustomerBalanceSnapshot? balanceSnapshot = null;
        decimal projectedBalance = 0m;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (!isAdminOrManager)
            {
                var visitLockAcquired = await _visitLifecycleLockService.LockAsync(order.VisitId);
                if (!visitLockAcquired)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult
                    {
                        Status = OrderWorkflowStatus.VisitNotFound,
                        Order = order
                    };
                }

                var lockedVisit = await _context.Visits.FirstOrDefaultAsync(x => x.Id == order.VisitId);
                if (lockedVisit is null)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult
                    {
                        Status = OrderWorkflowStatus.VisitNotFound,
                        Order = order
                    };
                }

                if (lockedVisit.Status != VisitStatus.InProgress)
                {
                    await transaction.RollbackAsync();
                    return new OrderWorkflowResult
                    {
                        Status = OrderWorkflowStatus.SalesRepOrderCancellationWindowClosed,
                        Order = order
                    };
                }
            }

            var customerLockAcquired = await _customerFinancialLockService.LockAsync(order.CustomerId);
            if (!customerLockAcquired)
            {
                await transaction.RollbackAsync();

                return new OrderWorkflowResult
                {
                    Status = OrderWorkflowStatus.CustomerNotFound,
                    Order = order
                };
            }

            balanceSnapshot = await _customerBalanceService.GetSnapshotAsync(order.CustomerId);
            if (balanceSnapshot is null)
            {
                await transaction.RollbackAsync();

                return new OrderWorkflowResult
                {
                    Status = OrderWorkflowStatus.CustomerNotFound,
                    Order = order
                };
            }

            projectedBalance = balanceSnapshot.CurrentBalance - order.TotalAmount;
            if (projectedBalance < 0)
            {
                await transaction.RollbackAsync();

                return new OrderWorkflowResult
                {
                    Status = OrderWorkflowStatus.OrderCancellationWouldCreateNegativeBalance,
                    Order = order,
                    CurrentBalance = balanceSnapshot.CurrentBalance,
                    ProjectedBalance = projectedBalance
                };
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelledByUserId = currentUser.Id;
            order.CancelledAtUtc = decisionTimeUtc;
            order.CancellationReason = normalizedReason;
            order.UpdatedAtUtc = decisionTimeUtc;

            _context.OrderActionHistories.Add(CreateHistoryEntry(
                orderId: order.Id,
                actionType: OrderActionType.Cancelled,
                previousStatus: previousStatus,
                newStatus: OrderStatus.Cancelled,
                performedByUser: currentUser,
                balanceBeforeAction: balanceSnapshot.CurrentBalance,
                balanceAfterAction: projectedBalance,
                comment: normalizedReason));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.OrderCancelled,
                nameof(Order),
                order.Id,
                $"Order '{order.OrderNumber}' for customer '{order.Customer.Name}' was cancelled by '{currentUser.FullName}'. Reason: {normalizedReason}. Balance before cancellation: {balanceSnapshot.CurrentBalance:0.00}. Balance after cancellation: {projectedBalance:0.00}.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            return new OrderWorkflowResult
            {
                Status = OrderWorkflowStatus.ConcurrencyConflict
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var updatedOrder = await LoadOrderAsync(orderId);

        return new OrderWorkflowResult
        {
            Status = OrderWorkflowStatus.Success,
            Order = updatedOrder,
            CurrentBalance = balanceSnapshot!.CurrentBalance,
            ProjectedBalance = projectedBalance
        };
    }

    private OrderActionHistory CreateHistoryEntry(
        Guid orderId,
        OrderActionType actionType,
        OrderStatus? previousStatus,
        OrderStatus newStatus,
        AppUser performedByUser,
        decimal? balanceBeforeAction,
        decimal? balanceAfterAction,
        string? comment)
    {
        return new OrderActionHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ActionType = actionType,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            PerformedByUserId = performedByUser.Id,
            PerformedByUserFullName = performedByUser.FullName,
            BalanceBeforeAction = balanceBeforeAction,
            BalanceAfterAction = balanceAfterAction,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            ActionAtUtc = DateTime.UtcNow
        };
    }

    private Task<Order?> LoadOrderAsync(Guid orderId)
    {
        return _context.Orders
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .Include(x => x.Visit)
            .Include(x => x.CancelledByUser)
            .Include(x => x.Items)
                .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == orderId);
    }
}