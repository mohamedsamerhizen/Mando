using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Payments;

public class VoidApprovedPaymentRequestDto
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
