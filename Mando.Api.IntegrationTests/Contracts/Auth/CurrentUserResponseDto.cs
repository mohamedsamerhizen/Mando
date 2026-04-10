namespace Mando.Api.IntegrationTests.Contracts.Auth;

public class CurrentUserResponseDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IList<string> Roles { get; set; } = [];
}
