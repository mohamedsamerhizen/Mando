using Mando.Api.Entities.Identity;

namespace Mando.Api.Interfaces.Common;

public interface ICurrentUserContext
{
    Task<AppUser?> GetCurrentUserAsync();
    Task<IReadOnlyList<string>> GetCurrentUserRolesAsync(AppUser? user = null);
}
