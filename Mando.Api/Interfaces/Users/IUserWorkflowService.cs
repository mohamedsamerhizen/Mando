using Mando.Api.DTOs.Users;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Users;

namespace Mando.Api.Interfaces.Users;

public interface IUserWorkflowService
{
    Task<UserWorkflowResult> CreateAsync(CreateUserRequestDto request, AppUser currentUser);

    Task<UserWorkflowResult> ChangeStatusAsync(
        Guid userId,
        ChangeUserStatusRequestDto request,
        AppUser? currentUser);

    Task<UserWorkflowResult> ChangeRoleAsync(
        Guid userId,
        ChangeUserRoleRequestDto request,
        AppUser? currentUser);
}
