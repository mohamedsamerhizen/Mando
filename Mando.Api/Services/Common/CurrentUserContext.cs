using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Mando.Api.Entities.Identity;
using Mando.Api.Interfaces.Common;

namespace Mando.Api.Services.Common;

public class CurrentUserContext : ICurrentUserContext
{
    private const string CurrentUserItemKey = "__CurrentUserContext.User";
    private const string CurrentUserRolesItemKey = "__CurrentUserContext.Roles";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<AppUser> _userManager;

    public CurrentUserContext(
        IHttpContextAccessor httpContextAccessor,
        UserManager<AppUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public async Task<AppUser?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return null;

        if (httpContext.Items.TryGetValue(CurrentUserItemKey, out var cachedUser) &&
            cachedUser is AppUser user)
        {
            return user.IsActive ? user : null;
        }

        var currentUserIdValue = ResolveCurrentUserId(httpContext.User);
        if (string.IsNullOrWhiteSpace(currentUserIdValue))
            return null;

        var currentUser = await _userManager.FindByIdAsync(currentUserIdValue);
        if (currentUser is null || !currentUser.IsActive)
            return null;

        httpContext.Items[CurrentUserItemKey] = currentUser;
        return currentUser;
    }

    public async Task<IReadOnlyList<string>> GetCurrentUserRolesAsync(AppUser? user = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return Array.Empty<string>();

        if (httpContext.Items.TryGetValue(CurrentUserRolesItemKey, out var cachedRoles) &&
            cachedRoles is IReadOnlyList<string> roles)
        {
            return roles;
        }

        user ??= await GetCurrentUserAsync();
        if (user is null)
            return Array.Empty<string>();

        var roleList = (await _userManager.GetRolesAsync(user)).ToArray();
        httpContext.Items[CurrentUserRolesItemKey] = roleList;

        return roleList;
    }

    private static string? ResolveCurrentUserId(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? principal.FindFirstValue("sub");
    }

}
