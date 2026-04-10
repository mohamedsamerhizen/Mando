using Mando.Api.DTOs.Common;

namespace Mando.Api.DTOs.Customers;

public class GetCustomersQueryDto : PagedQueryDto
{
    public string? Search { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
}