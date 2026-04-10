
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.Common;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Customers;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Customers;
using Mando.Api.Models.Customers;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : CurrentUserAwareControllerBase
{
    private readonly ICustomerWorkflowService _customerWorkflowService;
    private readonly ICustomerQueryService _customerQueryService;

    public CustomersController(
        ICurrentUserContext currentUserContext,
        ICustomerWorkflowService customerWorkflowService,
        ICustomerQueryService customerQueryService)
        : base(currentUserContext)
    {
        _customerWorkflowService = customerWorkflowService;
        _customerQueryService = customerQueryService;
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<CustomerResponseDto>> Create(CreateCustomerRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _customerWorkflowService.CreateAsync(request, currentUser);

        return result.Status switch
        {
            CustomerWorkflowStatus.Success => CreatedAtAction(
                nameof(GetById),
                new { id = result.Customer!.Id },
                MapCustomer(result.Customer!, result.AssignedSalesRepName!)),

            CustomerWorkflowStatus.CustomerNameRequired => ApiResponseFactory.BadRequest(
                this,
                "customer_name_required",
                "Customer name is required."),

            CustomerWorkflowStatus.CustomerCodeRequired => ApiResponseFactory.BadRequest(
                this,
                "customer_code_required",
                "Customer code is required."),

            CustomerWorkflowStatus.InvalidGeoCoordinates => ApiResponseFactory.BadRequest(
                this,
                "invalid_geo_coordinates",
                "Latitude must be between -90 and 90, and longitude must be between -180 and 180."),

            CustomerWorkflowStatus.AssignedSalesRepNotFound => ApiResponseFactory.BadRequest(
                this,
                "assigned_sales_rep_not_found",
                "Assigned sales rep was not found."),

            CustomerWorkflowStatus.AssignedUserNotSalesRep => ApiResponseFactory.BadRequest(
                this,
                "assigned_user_not_sales_rep",
                "Assigned user must have SalesRep role."),

            CustomerWorkflowStatus.AssignedSalesRepInactive => ApiResponseFactory.BadRequest(
                this,
                "assigned_sales_rep_inactive",
                "Assigned sales rep must be active."),

            CustomerWorkflowStatus.CustomerCodeAlreadyExists => ApiResponseFactory.BadRequest(
                this,
                "customer_code_already_exists",
                "Customer code already exists."),

            _ => Problem("Unexpected customer create workflow result.")
        };
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<CustomerResponseDto>>> GetAll([FromQuery] GetCustomersQueryDto query)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _customerQueryService.GetAllAsync(query, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponseDto>> GetById(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _customerQueryService.GetByIdAsync(id, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<CustomerActionHistoryResponseDto>>> GetHistory(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _customerQueryService.GetHistoryAsync(id, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<CustomerResponseDto>> Update(Guid id, UpdateCustomerRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _customerWorkflowService.UpdateAsync(id, request, currentUser);

        return result.Status switch
        {
            CustomerWorkflowStatus.Success => Ok(MapCustomer(result.Customer!, result.AssignedSalesRepName!)),

            CustomerWorkflowStatus.CustomerNotFound => ApiResponseFactory.NotFound(
                this,
                "customer_not_found",
                "Customer was not found."),

            CustomerWorkflowStatus.CustomerNameRequired => ApiResponseFactory.BadRequest(
                this,
                "customer_name_required",
                "Customer name is required."),

            CustomerWorkflowStatus.CustomerCodeRequired => ApiResponseFactory.BadRequest(
                this,
                "customer_code_required",
                "Customer code is required."),

            CustomerWorkflowStatus.InvalidGeoCoordinates => ApiResponseFactory.BadRequest(
                this,
                "invalid_geo_coordinates",
                "Latitude must be between -90 and 90, and longitude must be between -180 and 180."),

            CustomerWorkflowStatus.AssignedSalesRepNotFound => ApiResponseFactory.BadRequest(
                this,
                "assigned_sales_rep_not_found",
                "Assigned sales rep was not found."),

            CustomerWorkflowStatus.AssignedUserNotSalesRep => ApiResponseFactory.BadRequest(
                this,
                "assigned_user_not_sales_rep",
                "Assigned user must have SalesRep role."),

            CustomerWorkflowStatus.AssignedSalesRepInactive => ApiResponseFactory.BadRequest(
                this,
                "assigned_sales_rep_inactive",
                "Assigned sales rep must be active."),

            CustomerWorkflowStatus.CustomerCodeAlreadyExists => ApiResponseFactory.BadRequest(
                this,
                "customer_code_already_exists",
                "Customer code already exists."),

            CustomerWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is invalid."),

            CustomerWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "concurrency_conflict",
                "The customer was modified by another user. Refresh and try again."),

            _ => Problem("Unexpected customer update workflow result.")
        };
    }

    [HttpPatch("{id:guid}/financial-settings")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<CustomerResponseDto>> AdjustFinancialSettings(
        Guid id,
        AdjustCustomerFinancialSettingsRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _customerWorkflowService.AdjustFinancialSettingsAsync(id, request, currentUser);

        return result.Status switch
        {
            CustomerWorkflowStatus.Success => Ok(MapCustomer(result.Customer!, result.AssignedSalesRepName!)),

            CustomerWorkflowStatus.CustomerNotFound => ApiResponseFactory.NotFound(
                this,
                "customer_not_found",
                "Customer was not found."),

            CustomerWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is invalid."),

            CustomerWorkflowStatus.CustomerFinancialAdjustmentReasonRequired => ApiResponseFactory.BadRequest(
                this,
                "customer_financial_adjustment_reason_required",
                "Reason is required when adjusting customer financial settings."),

            CustomerWorkflowStatus.CustomerFinancialSettingsUnchanged => ApiResponseFactory.BadRequest(
                this,
                "customer_financial_settings_unchanged",
                "No financial change was detected."),

            CustomerWorkflowStatus.CustomerOpeningBalanceAdjustmentWouldCreateNegativeBalance => ApiResponseFactory.BadRequest(
                this,
                "customer_opening_balance_adjustment_would_create_negative_balance",
                "Opening balance adjustment would create a negative customer balance, which is not allowed."),

            CustomerWorkflowStatus.CustomerCreditLimitWouldFallBelowProjectedOutstandingBalance => ApiResponseFactory.BadRequest(
                this,
                "customer_credit_limit_would_fall_below_projected_outstanding_balance",
                "Credit limit cannot be set below the customer's projected outstanding balance."),

            CustomerWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "concurrency_conflict",
                "The customer was modified by another user. Refresh and try again."),

            _ => Problem("Unexpected customer financial adjustment workflow result.")
        };
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<CustomerResponseDto>> ChangeStatus(Guid id, ChangeCustomerStatusRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _customerWorkflowService.ChangeStatusAsync(id, request, currentUser);

        return result.Status switch
        {
            CustomerWorkflowStatus.Success => Ok(MapCustomer(result.Customer!, result.AssignedSalesRepName!)),

            CustomerWorkflowStatus.CustomerNotFound => ApiResponseFactory.NotFound(
                this,
                "customer_not_found",
                "Customer was not found."),

            CustomerWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is invalid."),

            CustomerWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "concurrency_conflict",
                "The customer was modified by another user. Refresh and try again."),

            CustomerWorkflowStatus.CustomerStatusReasonRequired => ApiResponseFactory.BadRequest(
                this,
                "customer_status_reason_required",
                "Reason is required when changing customer status."),

            CustomerWorkflowStatus.CustomerStatusUnchanged => ApiResponseFactory.BadRequest(
                this,
                "customer_status_unchanged",
                "Customer status is already set to the requested value."),

            CustomerWorkflowStatus.CustomerHasInProgressVisit => ApiResponseFactory.BadRequest(
                this,
                "customer_has_in_progress_visit",
                "Customer cannot be deactivated while an in-progress visit exists."),

            CustomerWorkflowStatus.CustomerHasPendingPayments => ApiResponseFactory.BadRequest(
                this,
                "customer_has_pending_payments",
                "Customer cannot be deactivated while pending payments exist."),

            CustomerWorkflowStatus.CustomerHasSubmittedOrders => ApiResponseFactory.BadRequest(
                this,
                "customer_has_submitted_orders",
                "Customer cannot be deactivated while submitted orders exist."),

            _ => Problem("Unexpected customer status workflow result.")
        };
    }

    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<CustomerBalanceDto>> GetBalance(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _customerQueryService.GetBalanceAsync(id, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}/statement")]
    public async Task<ActionResult<CustomerStatementResponseDto>> GetStatement(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _customerQueryService.GetStatementAsync(id, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}/financial-ledger")]
    public async Task<ActionResult<CustomerFinancialLedgerResponseDto>> GetFinancialLedger(
        Guid id,
        [FromQuery] GetCustomerFinancialLedgerQueryDto query)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _customerQueryService.GetFinancialLedgerAsync(id, query, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}/credit-profile")]
    public async Task<ActionResult<CustomerCreditProfileResponseDto>> GetCreditProfile(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _customerQueryService.GetCreditProfileAsync(id, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    private ActionResult<T> MapQueryResult<T>(CustomerQueryResult<T> result)
    {
        switch (result.Status)
        {
            case CustomerQueryStatus.Success:
                return Ok(result.Data);

            case CustomerQueryStatus.CustomerNotFound:
                return new ActionResult<T>(ApiResponseFactory.NotFound(
                    this,
                    "customer_not_found",
                    "Customer was not found."));

            case CustomerQueryStatus.Forbidden:
                return new ActionResult<T>(Forbid());

            default:
                return new ActionResult<T>(Problem("Unexpected customer query result."));
        }
    }

    private static CustomerResponseDto MapCustomer(Mando.Api.Entities.Customer customer, string assignedSalesRepName)
    {
        return new CustomerResponseDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Code = customer.Code,
            ContactPersonName = customer.ContactPersonName,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address,
            City = customer.City,
            Region = customer.Region,
            Latitude = customer.Latitude,
            Longitude = customer.Longitude,
            Status = customer.Status,
            CreditLimit = customer.CreditLimit,
            OpeningBalance = customer.OpeningBalance,
            Notes = customer.Notes,
            AssignedSalesRepId = customer.AssignedSalesRepId,
            AssignedSalesRepName = assignedSalesRepName,
            RowVersion = RowVersionTokenHelper.Encode(customer.RowVersion),
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc
        };
    }
}
