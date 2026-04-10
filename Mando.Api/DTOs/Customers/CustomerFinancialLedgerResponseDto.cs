namespace Mando.Api.DTOs.Customers;

public class CustomerFinancialLedgerResponseDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;

    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }

    public decimal OpeningBalanceAtRangeStart { get; set; }
    public decimal NetChangeInRange { get; set; }
    public decimal BalanceAtRangeEnd { get; set; }
    public decimal CurrentBalance { get; set; }

    public int TotalEntriesInRange { get; set; }
    public int ReturnedEntries { get; set; }
    public bool IsTruncated { get; set; }

    public List<CustomerFinancialLedgerEntryDto> Entries { get; set; } = [];
}