namespace Mando.Api.DTOs.Users;

public class SalesRepLookupDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}