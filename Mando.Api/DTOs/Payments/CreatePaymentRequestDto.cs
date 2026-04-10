using System.ComponentModel.DataAnnotations;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Payments;

public class CreatePaymentRequestDto
{
    [Required]
    public Guid VisitId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod PaymentMethod { get; set; }

    [MaxLength(150)]
    public string? Reference { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}