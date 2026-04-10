namespace Mando.Api.DTOs.Operations;

public class OperationsDashboardResponseDto
{
    public DateTime DateFromUtc { get; set; }
    public DateTime DateToUtc { get; set; }

    public int TotalVisits { get; set; }
    public int CompletedVisits { get; set; }
    public int InProgressVisits { get; set; }
    public int CancelledVisits { get; set; }

    public int TotalOrders { get; set; }
    public decimal TotalSalesAmount { get; set; }

    public int TotalPayments { get; set; }
    public int PendingPayments { get; set; }
    public int ApprovedPaymentsCount { get; set; }
    public int RejectedPaymentsCount { get; set; }
    public decimal ApprovedPaymentsAmount { get; set; }

    public List<OperationVisitSummaryDto> Visits { get; set; } = [];
    public List<OperationOrderSummaryDto> Orders { get; set; } = [];
    public List<OperationPaymentSummaryDto> Payments { get; set; } = [];
}