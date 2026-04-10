using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Payments;

public class ApprovePaymentRequestDto
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string ReviewComment { get; set; } = string.Empty;

    public bool AcknowledgeStalePayment { get; set; }
    public bool AcknowledgeHighBalanceImpact { get; set; }
    public bool AcknowledgeMultiplePendingPayments { get; set; }
    public bool AcknowledgeDuplicateReference { get; set; }
}