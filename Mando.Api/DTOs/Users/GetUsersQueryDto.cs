using Mando.Api.DTOs.Common;

namespace Mando.Api.DTOs.Users;

public class GetUsersQueryDto : PagedQueryDto
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}