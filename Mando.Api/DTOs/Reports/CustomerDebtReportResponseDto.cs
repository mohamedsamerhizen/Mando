namespace Mando.Api.DTOs.Reports;

public class CustomerDebtReportResponseDto
{
    public DateTime DateFromUtc { get; set; }
    public DateTime DateToUtc { get; set; }
    public Guid? SalesRepId { get; set; }
    public bool PositiveBalanceOnly { get; set; }

    public int TotalCustomers { get; set; }
    public int CustomersWithDebtCount { get; set; }

    public decimal TotalOpeningBalance { get; set; }
    public decimal TotalOrders { get; set; }
    public decimal TotalApprovedPayments { get; set; }
    public decimal TotalCurrentBalance { get; set; }

    public List<CustomerDebtReportItemDto> Items { get; set; } = [];
}