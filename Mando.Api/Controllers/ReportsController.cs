using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.Common;
using Mando.Api.DTOs.Reports;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Reports;
using Mando.Api.Models.Reports;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
public class ReportsController : ControllerBase
{
    private readonly IReportQueryService _reportQueryService;

    public ReportsController(IReportQueryService reportQueryService)
    {
        _reportQueryService = reportQueryService;
    }

    [HttpGet("sales-by-rep")]
    public async Task<ActionResult<List<SalesByRepDto>>> GetSalesByRep([FromQuery] GetReportDateRangeQueryDto query)
    {
        var result = await _reportQueryService.GetSalesByRepAsync(query);
        return MapResult(result);
    }

    [HttpGet("collections-by-rep")]
    public async Task<ActionResult<List<CollectionsByRepDto>>> GetCollectionsByRep([FromQuery] GetReportDateRangeQueryDto query)
    {
        var result = await _reportQueryService.GetCollectionsByRepAsync(query);
        return MapResult(result);
    }

    [HttpGet("customer-balances")]
    public async Task<ActionResult<List<CustomerBalanceReportDto>>> GetCustomerBalances()
    {
        var result = await _reportQueryService.GetCustomerBalancesAsync();
        return MapResult(result);
    }

    [HttpGet("top-debt-customers")]
    public async Task<ActionResult<List<CustomerBalanceReportDto>>> GetTopDebtCustomers()
    {
        var result = await _reportQueryService.GetTopDebtCustomersAsync();
        return MapResult(result);
    }

    [HttpGet("visit-attempts")]
    public async Task<ActionResult<List<VisitAttemptReportDto>>> GetVisitAttempts()
    {
        var result = await _reportQueryService.GetVisitAttemptsAsync();
        return MapResult(result);
    }

    [HttpGet("sales-reps-visit-compliance")]
    public async Task<ActionResult<List<SalesRepVisitComplianceDto>>> GetSalesRepsVisitCompliance()
    {
        var result = await _reportQueryService.GetSalesRepsVisitComplianceAsync();
        return MapResult(result);
    }

    private ActionResult<T> MapResult<T>(ReportQueryResult<T> result)
    {
        return result.Status switch
        {
            Mando.Api.Enums.ReportQueryStatus.Success => Ok(result.Data),
            _ => new ActionResult<T>(Problem("Unexpected reports query result."))
        };
    }
}