using Mando.Api.DTOs.Reports;
using Mando.Api.Models.Reports;

namespace Mando.Api.Interfaces.Reports;

public interface IReportQueryService
{
    Task<ReportQueryResult<List<SalesByRepDto>>> GetSalesByRepAsync(GetReportDateRangeQueryDto query);

    Task<ReportQueryResult<List<CollectionsByRepDto>>> GetCollectionsByRepAsync(GetReportDateRangeQueryDto query);

    Task<ReportQueryResult<List<CustomerBalanceReportDto>>> GetCustomerBalancesAsync();

    Task<ReportQueryResult<List<CustomerBalanceReportDto>>> GetTopDebtCustomersAsync();

    Task<ReportQueryResult<List<VisitAttemptReportDto>>> GetVisitAttemptsAsync();

    Task<ReportQueryResult<List<SalesRepVisitComplianceDto>>> GetSalesRepsVisitComplianceAsync();
}