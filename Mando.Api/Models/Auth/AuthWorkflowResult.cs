using Mando.Api.DTOs.Auth;
using Mando.Api.Enums;

namespace Mando.Api.Models.Auth;

public sealed class AuthWorkflowResult
{
    public AuthWorkflowStatus Status { get; init; }
    public LoginResponseDto? Response { get; init; }
}