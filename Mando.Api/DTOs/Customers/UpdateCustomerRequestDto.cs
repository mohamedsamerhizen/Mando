using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Customers;

public class UpdateCustomerRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ContactPersonName { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Required]
    public Guid AssignedSalesRepId { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
