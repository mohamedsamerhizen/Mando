using Mando.Api.Enums;

namespace Mando.Api.DTOs.Customers;

public class CustomerResponseDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string? ContactPersonName { get; set; }
    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    public CustomerStatus Status { get; set; }

    public decimal CreditLimit { get; set; }
    public decimal OpeningBalance { get; set; }

    public string? Notes { get; set; }

    public Guid AssignedSalesRepId { get; set; }
    public string AssignedSalesRepName { get; set; } = string.Empty;

    public string RowVersion { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}