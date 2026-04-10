using System.ComponentModel.DataAnnotations;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Orders;

public class CreateOrderRequestDto
{
    [Required]
    public Guid VisitId { get; set; }

    [EnumDataType(typeof(PaymentType))]
    public PaymentType PaymentType { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateOrderItemRequestDto> Items { get; set; } = [];
}