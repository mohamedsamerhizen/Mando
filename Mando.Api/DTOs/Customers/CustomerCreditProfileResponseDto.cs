using Mando.Api.Enums;

namespace Mando.Api.DTOs.Customers;

public class CustomerCreditProfileResponseDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;

    public CustomerStatus CustomerStatus { get; set; }

    public Guid AssignedSalesRepId { get; set; }
    public string AssignedSalesRepName { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }
    public decimal TotalOrders { get; set; }
    public decimal ApprovedPayments { get; set; }
    public decimal CurrentBalance { get; set; }

    public decimal CreditLimit { get; set; }
    public decimal? RemainingCredit { get; set; }
    public decimal? CreditUtilizationRatio { get; set; }
    public decimal? OverLimitAmount { get; set; }

    public CustomerCreditExposureLevel ExposureLevel { get; set; }
    public bool RequiresAdministrativeAttention { get; set; }

    public bool CanStartVisit { get; set; }
    public bool CanCreateOrder { get; set; }
    public bool CanCreatePayment { get; set; }

    public bool HasInProgressVisit { get; set; }

    public int PendingPaymentsCount { get; set; }
    public decimal PendingPaymentsAmount { get; set; }

    public int ApprovalBlockedPendingPaymentsCount { get; set; }
    public decimal ApprovalBlockedPendingPaymentsAmount { get; set; }

    public DateTime? LastOrderDateUtc { get; set; }
    public DateTime? LastApprovedPaymentDateUtc { get; set; }
    public int? DaysSinceLastApprovedPayment { get; set; }

    public string RecommendedAction { get; set; } = string.Empty;
}