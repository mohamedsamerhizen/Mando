using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Users;

public class ChangeUserStatusRequestDto
{
    public bool IsActive { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
