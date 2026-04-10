using Mando.Api.DTOs.Common;

namespace Mando.Api.DTOs.Payments;

public class PaymentReviewQueueResponseDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public int StaleAfterHours { get; set; }
    public PaymentReviewQueueSummaryDto Summary { get; set; } = new();
    public PagedResultDto<PaymentReviewQueueItemDto> Queue { get; set; } = new();
}