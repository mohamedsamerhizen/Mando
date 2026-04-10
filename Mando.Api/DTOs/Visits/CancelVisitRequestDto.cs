using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Visits;

public class CancelVisitRequestDto
{
    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}