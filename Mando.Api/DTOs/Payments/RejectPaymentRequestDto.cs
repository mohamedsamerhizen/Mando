using System.ComponentModel.DataAnnotations;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Payments;

public class RejectPaymentRequestDto
{
    [Required]
    [EnumDataType(typeof(PaymentRejectionCategory))]
    public PaymentRejectionCategory? Category { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}