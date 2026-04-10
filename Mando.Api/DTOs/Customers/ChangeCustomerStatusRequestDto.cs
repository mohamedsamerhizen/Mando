using System.ComponentModel.DataAnnotations;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Customers;

public class ChangeCustomerStatusRequestDto
{
    [EnumDataType(typeof(CustomerStatus))]
    public CustomerStatus Status { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}