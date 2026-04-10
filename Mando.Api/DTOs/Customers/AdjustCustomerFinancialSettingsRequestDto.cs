using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Customers;

public class AdjustCustomerFinancialSettingsRequestDto
{
    [Range(0, double.MaxValue)]
    public decimal CreditLimit { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OpeningBalance { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}