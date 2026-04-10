using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Models.Users;

public sealed class UserWorkflowResult
{
    public UserWorkflowStatus Status { get; init; }
    public AppUser? User { get; init; }
    public List<string> Roles { get; init; } = [];
    public string[] IdentityErrors { get; init; } = [];
}