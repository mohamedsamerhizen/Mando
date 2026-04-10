using Mando.Api.DTOs.Operations;
using Mando.Api.Models.Operations;

namespace Mando.Api.Interfaces.Operations;

public interface IOperationsQueryService
{
    Task<OperationsQueryResult<OperationsDashboardResponseDto>> GetTodayDashboardAsync(
        Guid? salesRepId,
        Guid? customerId,
        Mando.Api.Enums.VisitStatus? visitStatus,
        Mando.Api.Enums.PaymentStatus? paymentStatus,
        bool includeVisits,
        bool includeOrders,
        bool includePayments,
        int itemsLimit);

    Task<OperationsQueryResult<OperationsDashboardResponseDto>> GetRangeDashboardAsync(
        GetOperationsDashboardQueryDto query);

    Task<OperationsQueryResult<OperationsKpiDashboardResponseDto>> GetTodayKpisAsync(
        int topCount);

    Task<OperationsQueryResult<OperationsKpiDashboardResponseDto>> GetRangeKpisAsync(
        GetOperationsKpiQueryDto query);

    Task<OperationsQueryResult<UnifiedOperationsDashboardResponseDto>> GetUnifiedDashboardAsync(
        GetUnifiedOperationsDashboardQueryDto query);

    Task<OperationsQueryResult<OperationsAlertsResponseDto>> GetAlertsAsync(
        GetOperationsAlertsQueryDto query);

    Task<OperationsQueryResult<IReadOnlyList<OperationsAlertReviewDto>>> GetAlertReviewHistoryAsync(
        string alertFingerprint);
}