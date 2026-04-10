using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.Common;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Orders;
using Mando.Api.Entities;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Orders;
using Mando.Api.Models.Orders;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : CurrentUserAwareControllerBase
{
    private readonly IOrderWorkflowService _orderWorkflowService;
    private readonly IOrderQueryService _orderQueryService;

    public OrdersController(
        ICurrentUserContext currentUserContext,
        IOrderWorkflowService orderWorkflowService,
        IOrderQueryService orderQueryService)
        : base(currentUserContext)
    {
        _orderWorkflowService = orderWorkflowService;
        _orderQueryService = orderQueryService;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.SalesRep)]
    public async Task<ActionResult<OrderResponseDto>> Create(CreateOrderRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _orderWorkflowService.CreateAsync(request, currentUser);

        return result.Status switch
        {
            OrderWorkflowStatus.Success => CreatedAtAction(
                nameof(GetById),
                new { id = result.Order!.Id },
                MapOrder(result.Order!)),

            OrderWorkflowStatus.VisitNotFound => ApiResponseFactory.BadRequest(
                this,
                "visit_not_found",
                "Visit was not found."),

            OrderWorkflowStatus.Forbidden => Forbid(),

            OrderWorkflowStatus.VisitNotInProgress => ApiResponseFactory.BadRequest(
                this,
                "visit_not_in_progress",
                "Order can only be created during an active visit."),

            OrderWorkflowStatus.CustomerInactive => ApiResponseFactory.BadRequest(
                this,
                "customer_inactive",
                "Order cannot be created for a non-active customer."),

            OrderWorkflowStatus.OrderItemsRequired => ApiResponseFactory.BadRequest(
                this,
                "order_items_required",
                "Order must contain at least one item."),

            OrderWorkflowStatus.InvalidOrInactiveProducts => ApiResponseFactory.BadRequest(
                this,
                "invalid_or_inactive_products",
                "One or more products are invalid or inactive."),

            OrderWorkflowStatus.InvalidQuantity => ApiResponseFactory.BadRequest(
                this,
                "invalid_quantity",
                "Quantity must be greater than zero."),

            OrderWorkflowStatus.DuplicateProductsNotAllowed => ApiResponseFactory.BadRequest(
                this,
                "duplicate_products_not_allowed",
                "The same product cannot appear more than once in the same order."),

            OrderWorkflowStatus.CreditLimitExceeded => ApiResponseFactory.Conflict(
                this,
                "credit_limit_exceeded",
                $"Credit limit exceeded. Current balance: {result.CurrentBalance:0.00}, projected balance: {result.ProjectedBalance:0.00}, credit limit: {result.CreditLimit:0.00}."),

            _ => Problem("Unexpected order workflow result.")
        };
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager},{AppRoles.SalesRep}")]
    public async Task<ActionResult<OrderResponseDto>> Cancel(Guid id, CancelOrderRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _orderWorkflowService.CancelAsync(id, request, currentUser, currentUserRoles);

        return result.Status switch
        {
            OrderWorkflowStatus.Success => Ok(MapOrder(result.Order!)),

            OrderWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is required and must be a valid Base64 value."),

            OrderWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "order_concurrency_conflict",
                "Order was changed by another user. Refresh and retry."),

            OrderWorkflowStatus.CancellationReasonRequired => ApiResponseFactory.BadRequest(
                this,
                "cancellation_reason_required",
                "Cancellation reason is required."),

            OrderWorkflowStatus.OrderNotFound => ApiResponseFactory.NotFound(
                this,
                "order_not_found",
                "Order was not found."),

            OrderWorkflowStatus.Forbidden => Forbid(),

            OrderWorkflowStatus.OrderAlreadyCancelled => ApiResponseFactory.BadRequest(
                this,
                "order_already_cancelled",
                "Order is already cancelled."),

            OrderWorkflowStatus.SalesRepOrderCancellationWindowClosed => ApiResponseFactory.BadRequest(
                this,
                "sales_rep_order_cancellation_window_closed",
                "Sales rep can cancel only while the related visit is still in progress."),

            OrderWorkflowStatus.OrderCancellationWouldCreateNegativeBalance => ApiResponseFactory.Conflict(
                this,
                "order_cancellation_would_create_negative_balance",
                $"Order cannot be cancelled because the resulting balance would become negative. Current balance: {result.CurrentBalance:0.00}, projected balance: {result.ProjectedBalance:0.00}."),

            OrderWorkflowStatus.CustomerNotFound => ApiResponseFactory.NotFound(
                this,
                "customer_not_found",
                "Customer was not found."),

            _ => Problem("Unexpected order cancellation result.")
        };
    }

    [HttpGet("operations-report")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<OrderOperationsReportResponseDto>> GetOperationsReport(
        [FromQuery] GetOrderOperationsReportQueryDto query)
    {
        if (query.DateToUtc.HasValue && query.DateFromUtc.HasValue && query.DateToUtc.Value < query.DateFromUtc.Value)
        {
            return ApiResponseFactory.BadRequest(
                this,
                "invalid_report_range",
                "DateToUtc must be greater than or equal to DateFromUtc.");
        }

        var result = await _orderQueryService.GetOperationsReportAsync(query);
        return MapQueryResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<OrderResponseDto>>> GetAll([FromQuery] GetOrdersQueryDto query)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _orderQueryService.GetAllAsync(query, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponseDto>> GetById(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _orderQueryService.GetByIdAsync(id, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<OrderActionHistoryResponseDto>>> GetHistory(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _orderQueryService.GetHistoryAsync(id, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    private ActionResult<T> MapQueryResult<T>(OrderQueryResult<T> result)
    {
        switch (result.Status)
        {
            case OrderQueryStatus.Success:
                return Ok(result.Data);

            case OrderQueryStatus.OrderNotFound:
                return new ActionResult<T>(ApiResponseFactory.NotFound(
                    this,
                    "order_not_found",
                    "Order was not found."));

            case OrderQueryStatus.Forbidden:
                return new ActionResult<T>(Forbid());

            default:
                return new ActionResult<T>(Problem("Unexpected order query result."));
        }
    }

    private static OrderResponseDto MapOrder(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            VisitId = order.VisitId,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer.Name,
            SalesRepId = order.SalesRepId,
            SalesRepName = order.SalesRep.FullName,
            PaymentType = order.PaymentType,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Notes = order.Notes,
            CancelledByUserId = order.CancelledByUserId,
            CancelledByUserName = order.CancelledByUser?.FullName,
            CancelledAtUtc = order.CancelledAtUtc,
            CancellationReason = order.CancellationReason,
            RowVersion = RowVersionTokenHelper.Encode(order.RowVersion),
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            Items = order.Items.Select(item => new OrderItemResponseDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            }).ToList()
        };
    }
}