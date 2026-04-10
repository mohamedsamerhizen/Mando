using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Mando.Api.DTOs.Auth;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Auth;
using Mando.Api.Models.Auth;

namespace Mando.Api.Services.Auth;

public class AuthWorkflowService : IAuthWorkflowService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthWorkflowService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    public async Task<AuthWorkflowResult> LoginAsync(LoginRequestDto request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            return new AuthWorkflowResult
            {
                Status = AuthWorkflowStatus.InvalidCredentials
            };
        }

        if (!user.IsActive)
        {
            return new AuthWorkflowResult
            {
                Status = AuthWorkflowStatus.UserInactive
            };
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return new AuthWorkflowResult
            {
                Status = AuthWorkflowStatus.LockedOut
            };
        }

        if (!signInResult.Succeeded)
        {
            return new AuthWorkflowResult
            {
                Status = AuthWorkflowStatus.InvalidCredentials
            };
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = await _tokenService.CreateTokenAsync(user);

        return new AuthWorkflowResult
        {
            Status = AuthWorkflowStatus.Success,
            Response = new LoginResponseDto
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = roles
            }
        };
    }

    public async Task<CurrentUserResponseDto?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new CurrentUserResponseDto
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            Roles = roles
        };
    }
}
