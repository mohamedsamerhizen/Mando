using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Customers;

public class GetCustomerFinancialLedgerQueryDto
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }

    [Range(1, 500)]
    public int Take { get; set; } = 200;
}