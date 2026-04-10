using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Common;

public class PagedQueryDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 20;
}