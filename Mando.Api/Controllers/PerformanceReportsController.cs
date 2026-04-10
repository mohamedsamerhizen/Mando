using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.Common;
using Mando.Api.DTOs.Reports;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Reports;
using Mando.Api.Models.Reports;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/reports/performance")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
public class PerformanceReportsController : ControllerBase
{
    private readonly IPerformanceReportQueryService _performanceReportQueryService;

    public PerformanceReportsController(IPerformanceReportQueryService performanceReportQueryService)
    {
        _performanceReportQueryService = performanceReportQueryService;
    }

    [HttpGet("sales-reps")]
    public async Task<ActionResult<SalesRepPerformanceReportResponseDto>> GetSalesRepPerformance(
        [FromQuery] GetSalesRepPerformanceReportQueryDto query)
    {
        var result = await _performanceReportQueryService.GetSalesRepPerformanceAsync(query);
        return MapResult(result);
    }

    [HttpGet("customer-debt")]
    public async Task<ActionResult<CustomerDebtReportResponseDto>> GetCustomerDebtReport(
        [FromQuery] GetCustomerDebtReportQueryDto query)
    {
        var result = await _performanceReportQueryService.GetCustomerDebtReportAsync(query);
        return MapResult(result);
    }

    [HttpGet("visit-compliance")]
    public async Task<ActionResult<VisitComplianceReportResponseDto>> GetVisitComplianceReport(
        [FromQuery] GetVisitComplianceReportQueryDto query)
    {
        var result = await _performanceReportQueryService.GetVisitComplianceReportAsync(query);
        return MapResult(result);
    }

    [HttpGet("collections-by-sales-rep")]
    public async Task<ActionResult<CollectionsBySalesRepReportResponseDto>> GetCollectionsBySalesRep(
        [FromQuery] GetCollectionsBySalesRepReportQueryDto query)
    {
        var result = await _performanceReportQueryService.GetCollectionsBySalesRepAsync(query);
        return MapResult(result);
    }

    private ActionResult<T> MapResult<T>(PerformanceReportQueryResult<T> result)
    {
        return result.Status switch
        {
            Mando.Api.Enums.PerformanceReportQueryStatus.Success => Ok(result.Data),
            Mando.Api.Enums.PerformanceReportQueryStatus.ValidationError => ApiResponseFactory.BadRequest(this, "validation_error", result.ValidationMessage ?? "The request is invalid."),
            _ => new ActionResult<T>(Problem("Unexpected performance reports query result."))
        };
    }
}