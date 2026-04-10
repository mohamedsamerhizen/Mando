using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class UserActionHistory : BaseEntity
{
    public Guid TargetUserId { get; set; }
    public AppUser TargetUser { get; set; } = default!;

    public UserActionType ActionType { get; set; }

    public string FullNameSnapshot { get; set; } = string.Empty;
    public string EmailSnapshot { get; set; } = string.Empty;
    public string RolesSnapshot { get; set; } = string.Empty;

    public bool? PreviousIsActive { get; set; }
    public bool NewIsActive { get; set; }

    public Guid PerformedByUserId { get; set; }
    public AppUser PerformedByUser { get; set; } = default!;

    public string PerformedByUserFullName { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public DateTime ActionAtUtc { get; set; } = DateTime.UtcNow;
}