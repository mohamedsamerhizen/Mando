using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.DTOs.Auth;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Auth;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthWorkflowService _authWorkflowService;

    public AuthController(IAuthWorkflowService authWorkflowService)
    {
        _authWorkflowService = authWorkflowService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authWorkflowService.LoginAsync(request);

        return result.Status switch
        {
            AuthWorkflowStatus.Success => Ok(result.Response),

            AuthWorkflowStatus.InvalidCredentials => ApiResponseFactory.Unauthorized(
                this,
                "invalid_credentials",
                "Invalid email or password."),

            AuthWorkflowStatus.UserInactive => ApiResponseFactory.Unauthorized(
                this,
                "user_inactive",
                "User account is inactive."),

            AuthWorkflowStatus.LockedOut => new ObjectResult(ApiResponseFactory.Build(
                HttpContext,
                "account_locked",
                "Too many failed login attempts. Try again later or contact an administrator."))
            {
                StatusCode = StatusCodes.Status423Locked
            },

            _ => Problem("Unexpected auth workflow result.")
        };
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponseDto>> GetCurrentUser()
    {
        var currentUser = await _authWorkflowService.GetCurrentUserAsync(User);
        if (currentUser is null)
        {
            return ApiResponseFactory.Unauthorized(
                this,
                "authenticated_user_not_found",
                "Authenticated user could not be resolved.");
        }

        return Ok(ApiResponseFactory.BuildSuccess(
            HttpContext,
            currentUser,
            message: "Current user loaded successfully.",
            code: "current_user_retrieved"));
    }
}
