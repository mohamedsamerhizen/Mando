
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.Common;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Users;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Users;
using Mando.Api.Models.Users;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : CurrentUserAwareControllerBase
{
    private readonly IUserWorkflowService _userWorkflowService;
    private readonly IUserQueryService _userQueryService;

    public UsersController(
        ICurrentUserContext currentUserContext,
        IUserWorkflowService userWorkflowService,
        IUserQueryService userQueryService)
        : base(currentUserContext)
    {
        _userWorkflowService = userWorkflowService;
        _userQueryService = userQueryService;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<UserResponseDto>> Create(CreateUserRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _userWorkflowService.CreateAsync(request, currentUser);

        return result.Status switch
        {
            UserWorkflowStatus.Success => CreatedAtAction(
                nameof(GetById),
                new { id = result.User!.Id },
                MapUser(result.User!, result.Roles)),

            UserWorkflowStatus.FullNameRequired => ApiResponseFactory.BadRequest(
                this,
                "full_name_required",
                "Full name is required."),

            UserWorkflowStatus.EmailRequired => ApiResponseFactory.BadRequest(
                this,
                "email_required",
                "Email is required."),

            UserWorkflowStatus.PasswordRequired => ApiResponseFactory.BadRequest(
                this,
                "password_required",
                "Password is required."),

            UserWorkflowStatus.RoleRequired => ApiResponseFactory.BadRequest(
                this,
                "role_required",
                "Role is required."),

            UserWorkflowStatus.InvalidRole => ApiResponseFactory.BadRequest(
                this,
                "invalid_role",
                "Invalid role."),

            UserWorkflowStatus.EmailAlreadyExists => ApiResponseFactory.BadRequest(
                this,
                "email_already_exists",
                "Email already exists."),

            UserWorkflowStatus.UserCreateFailed => new BadRequestObjectResult(ApiResponseFactory.Build(
                HttpContext,
                "user_create_failed",
                "User creation failed.",
                new Dictionary<string, string[]>
                {
                    ["Identity"] = result.IdentityErrors
                })),

            UserWorkflowStatus.AssignRoleFailed => new BadRequestObjectResult(ApiResponseFactory.Build(
                HttpContext,
                "assign_role_failed",
                "Assigning role to user failed.",
                new Dictionary<string, string[]>
                {
                    ["Identity"] = result.IdentityErrors
                })),

            _ => Problem("Unexpected user create workflow result.")
        };
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<PagedResultDto<UserResponseDto>>> GetAll([FromQuery] GetUsersQueryDto query)
    {
        var result = await _userQueryService.GetAllAsync(query);
        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<UserResponseDto>> GetById(Guid id)
    {
        var result = await _userQueryService.GetByIdAsync(id);
        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<IReadOnlyList<UserActionHistoryResponseDto>>> GetHistory(Guid id)
    {
        var result = await _userQueryService.GetHistoryAsync(id);
        return MapQueryResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<UserResponseDto>> ChangeStatus(Guid id, ChangeUserStatusRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _userWorkflowService.ChangeStatusAsync(id, request, currentUser);

        return result.Status switch
        {
            UserWorkflowStatus.Success => Ok(MapUser(result.User!, result.Roles)),

            UserWorkflowStatus.UserNotFound => ApiResponseFactory.NotFound(
                this,
                "user_not_found",
                "User was not found."),

            UserWorkflowStatus.CannotDeactivateAdmin => ApiResponseFactory.BadRequest(
                this,
                "cannot_deactivate_admin",
                "Admin user cannot be deactivated."),

            UserWorkflowStatus.UserStatusReasonRequired => ApiResponseFactory.BadRequest(
                this,
                "user_status_reason_required",
                "Reason is required when changing user status."),

            UserWorkflowStatus.UserStatusUnchanged => ApiResponseFactory.BadRequest(
                this,
                "user_status_unchanged",
                "User status is already set to the requested value."),

            UserWorkflowStatus.CannotDeactivateUserWithAssignedActiveCustomers => ApiResponseFactory.BadRequest(
                this,
                "cannot_deactivate_user_with_assigned_active_customers",
                "User cannot be deactivated while assigned active customers still exist."),

            UserWorkflowStatus.CannotDeactivateUserWithInProgressVisits => ApiResponseFactory.BadRequest(
                this,
                "cannot_deactivate_user_with_in_progress_visits",
                "User cannot be deactivated while in-progress visits still exist."),

            UserWorkflowStatus.CannotDeactivateUserWithPendingPayments => ApiResponseFactory.BadRequest(
                this,
                "cannot_deactivate_user_with_pending_payments",
                "User cannot be deactivated while pending payments still exist."),

            UserWorkflowStatus.CannotDeactivateUserWithSubmittedOrders => ApiResponseFactory.BadRequest(
                this,
                "cannot_deactivate_user_with_submitted_orders",
                "User cannot be deactivated while submitted orders still exist."),

            UserWorkflowStatus.UserUpdateFailed => new BadRequestObjectResult(ApiResponseFactory.Build(
                HttpContext,
                "user_update_failed",
                "User status update failed.",
                new Dictionary<string, string[]>
                {
                    ["Identity"] = result.IdentityErrors
                })),

            _ => Problem("Unexpected user status workflow result.")
        };
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<UserResponseDto>> ChangeRole(Guid id, ChangeUserRoleRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _userWorkflowService.ChangeRoleAsync(id, request, currentUser);

        return result.Status switch
        {
            UserWorkflowStatus.Success => Ok(MapUser(result.User!, result.Roles)),

            UserWorkflowStatus.UserNotFound => ApiResponseFactory.NotFound(
                this,
                "user_not_found",
                "User was not found."),

            UserWorkflowStatus.RoleRequired => ApiResponseFactory.BadRequest(
                this,
                "role_required",
                "Role is required."),

            UserWorkflowStatus.InvalidRole => ApiResponseFactory.BadRequest(
                this,
                "invalid_role",
                "Invalid role."),

            UserWorkflowStatus.UserRoleReasonRequired => ApiResponseFactory.BadRequest(
                this,
                "user_role_reason_required",
                "Reason is required when changing user role."),

            UserWorkflowStatus.UserRoleUnchanged => ApiResponseFactory.BadRequest(
                this,
                "user_role_unchanged",
                "User role is already set to the requested value."),

            UserWorkflowStatus.CannotChangeOwnRole => ApiResponseFactory.BadRequest(
                this,
                "cannot_change_own_role",
                "You cannot change your own role from this endpoint."),

            UserWorkflowStatus.CannotChangeAdminRole => ApiResponseFactory.BadRequest(
                this,
                "cannot_change_admin_role",
                "Admin user role cannot be changed through this endpoint."),

            UserWorkflowStatus.CannotChangeUserRoleWithAssignedActiveCustomers => ApiResponseFactory.BadRequest(
                this,
                "cannot_change_user_role_with_assigned_active_customers",
                "User role cannot be changed while assigned active customers still exist."),

            UserWorkflowStatus.CannotChangeUserRoleWithInProgressVisits => ApiResponseFactory.BadRequest(
                this,
                "cannot_change_user_role_with_in_progress_visits",
                "User role cannot be changed while in-progress visits still exist."),

            UserWorkflowStatus.CannotChangeUserRoleWithPendingPayments => ApiResponseFactory.BadRequest(
                this,
                "cannot_change_user_role_with_pending_payments",
                "User role cannot be changed while pending payments still exist."),

            UserWorkflowStatus.CannotChangeUserRoleWithSubmittedOrders => ApiResponseFactory.BadRequest(
                this,
                "cannot_change_user_role_with_submitted_orders",
                "User role cannot be changed while submitted orders still exist."),

            UserWorkflowStatus.AssignRoleFailed => new BadRequestObjectResult(ApiResponseFactory.Build(
                HttpContext,
                "assign_role_failed",
                "Changing the user's role failed.",
                new Dictionary<string, string[]>
                {
                    ["Identity"] = result.IdentityErrors
                })),

            UserWorkflowStatus.UserUpdateFailed => new BadRequestObjectResult(ApiResponseFactory.Build(
                HttpContext,
                "user_update_failed",
                "User role update failed.",
                new Dictionary<string, string[]>
                {
                    ["Identity"] = result.IdentityErrors
                })),

            _ => Problem("Unexpected user role workflow result.")
        };
    }

    [HttpGet("sales-reps")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<List<SalesRepLookupDto>>> GetSalesReps()
    {
        var result = await _userQueryService.GetSalesRepsAsync();
        return MapQueryResult(result);
    }

    private ActionResult<T> MapQueryResult<T>(UserQueryResult<T> result)
    {
        switch (result.Status)
        {
            case UserQueryStatus.Success:
                return Ok(result.Data);

            case UserQueryStatus.UserNotFound:
                return new ActionResult<T>(ApiResponseFactory.NotFound(
                    this,
                    "user_not_found",
                    "User was not found."));

            default:
                return new ActionResult<T>(Problem("Unexpected user query result."));
        }
    }

    private static UserResponseDto MapUser(AppUser user, List<string> roles)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            Roles = roles,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        };
    }
}

