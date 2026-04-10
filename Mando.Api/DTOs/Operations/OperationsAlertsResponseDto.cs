using Mando.Api.DTOs.Common;

namespace Mando.Api.DTOs.Operations;

public class OperationsAlertsResponseDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public int PaymentStaleAfterHours { get; set; }
    public int OrderStaleAfterHours { get; set; }
    public int VisitStaleAfterHours { get; set; }
    public decimal NearCreditLimitRatio { get; set; }

    public OperationsAlertsSummaryDto Summary { get; set; } = new();
    public PagedResultDto<OperationsAlertItemDto> Queue { get; set; } = new();
}