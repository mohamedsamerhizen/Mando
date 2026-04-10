using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Orders;

public class CreateOrderItemRequestDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }
}