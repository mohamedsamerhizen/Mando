using Mando.Api.DTOs.Orders;
using Mando.Api.DTOs.Payments;
using Mando.Api.DTOs.Visits;

namespace Mando.Api.DTOs.Operations;

public class UnifiedOperationsDashboardResponseDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public DateTime RangeFromUtc { get; set; }
    public DateTime RangeToUtc { get; set; }

    public UnifiedOperationsAttentionSummaryDto AttentionSummary { get; set; } = new();
    public UnifiedOperationsFlowSummaryDto FlowSummary { get; set; } = new();

    public PaymentOperationsReportResponseDto Payments { get; set; } = new();
    public OrderOperationsReportResponseDto Orders { get; set; } = new();
    public VisitOperationsReportResponseDto Visits { get; set; } = new();
}