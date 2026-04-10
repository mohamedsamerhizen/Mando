using Mando.Api.Enums;

namespace Mando.Api.DTOs.Users;

public class UserActionHistoryResponseDto
{
    public Guid Id { get; set; }
    public Guid TargetUserId { get; set; }

    public UserActionType ActionType { get; set; }

    public string FullNameSnapshot { get; set; } = string.Empty;
    public string EmailSnapshot { get; set; } = string.Empty;
    public string RolesSnapshot { get; set; } = string.Empty;

    public bool? PreviousIsActive { get; set; }
    public bool NewIsActive { get; set; }

    public Guid PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;

    public string? Comment { get; set; }
    public DateTime ActionAtUtc { get; set; }
}