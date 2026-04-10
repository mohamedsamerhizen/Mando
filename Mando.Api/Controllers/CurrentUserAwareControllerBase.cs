using Microsoft.AspNetCore.Mvc;
using Mando.Api.Entities.Identity;
using Mando.Api.Interfaces.Common;

namespace Mando.Api.Controllers;

public abstract class CurrentUserAwareControllerBase : ControllerBase
{
    private readonly ICurrentUserContext _currentUserContext;

    protected CurrentUserAwareControllerBase(ICurrentUserContext currentUserContext)
    {
        _currentUserContext = currentUserContext;
    }

    protected Task<AppUser?> GetCurrentUserAsync()
    {
        return _currentUserContext.GetCurrentUserAsync();
    }

    protected Task<IReadOnlyList<string>> GetCurrentUserRolesAsync(AppUser? user = null)
    {
        return _currentUserContext.GetCurrentUserRolesAsync(user);
    }
}

