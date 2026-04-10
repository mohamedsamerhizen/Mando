using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class Customer : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string? ContactPersonName { get; set; }
    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public decimal CreditLimit { get; set; }
    public decimal OpeningBalance { get; set; }

    public string? Notes { get; set; }

    public Guid AssignedSalesRepId { get; set; }
    public AppUser AssignedSalesRep { get; set; } = default!;

    public ICollection<CustomerActionHistory> ActionHistories { get; set; } = new List<CustomerActionHistory>();
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}