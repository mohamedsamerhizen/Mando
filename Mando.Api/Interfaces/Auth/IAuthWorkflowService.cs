using Mando.Api.DTOs.Auth;
using System.Security.Claims;
using Mando.Api.Models.Auth;

namespace Mando.Api.Interfaces.Auth;

public interface IAuthWorkflowService
{
    Task<AuthWorkflowResult> LoginAsync(LoginRequestDto request);
    Task<CurrentUserResponseDto?> GetCurrentUserAsync(ClaimsPrincipal principal);
}