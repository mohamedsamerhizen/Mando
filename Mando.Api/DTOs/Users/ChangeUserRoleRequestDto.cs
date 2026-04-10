using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Users;

public class ChangeUserRoleRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Reason { get; set; }
}