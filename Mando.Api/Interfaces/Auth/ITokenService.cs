using Mando.Api.Entities.Identity;

namespace Mando.Api.Interfaces.Auth;

public interface ITokenService
{
    Task<(string Token, DateTime ExpiresAtUtc)> CreateTokenAsync(AppUser user);
}