using Mando.Api.DTOs.Reports;
using Mando.Api.Models.Reports;

namespace Mando.Api.Interfaces.Reports;

public interface IPerformanceReportQueryService
{
    Task<PerformanceReportQueryResult<SalesRepPerformanceReportResponseDto>> GetSalesRepPerformanceAsync(
        GetSalesRepPerformanceReportQueryDto query);

    Task<PerformanceReportQueryResult<CustomerDebtReportResponseDto>> GetCustomerDebtReportAsync(
        GetCustomerDebtReportQueryDto query);

    Task<PerformanceReportQueryResult<VisitComplianceReportResponseDto>> GetVisitComplianceReportAsync(
        GetVisitComplianceReportQueryDto query);

    Task<PerformanceReportQueryResult<CollectionsBySalesRepReportResponseDto>> GetCollectionsBySalesRepAsync(
        GetCollectionsBySalesRepReportQueryDto query);
}