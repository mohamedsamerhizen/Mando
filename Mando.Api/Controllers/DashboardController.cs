using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.Common;
using Mando.Api.DTOs.Dashboard;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Dashboard;
using Mando.Api.Models.Dashboard;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : CurrentUserAwareControllerBase
{
    private readonly IDashboardQueryService _dashboardQueryService;

    public DashboardController(
        ICurrentUserContext currentUserContext,
        IDashboardQueryService dashboardQueryService)
        : base(currentUserContext)
    {
        _dashboardQueryService = dashboardQueryService;
    }

    [HttpGet("summary")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var result = await _dashboardQueryService.GetAdminSummaryAsync();
        return MapResult(result);
    }

    [HttpGet("my-summary")]
    [Authorize(Roles = AppRoles.SalesRep)]
    public async Task<ActionResult<SalesRepDashboardSummaryDto>> GetMySummary()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _dashboardQueryService.GetSalesRepSummaryAsync(currentUser);
        return MapResult(result);
    }

    private ActionResult<T> MapResult<T>(DashboardQueryResult<T> result)
    {
        return result.Status switch
        {
            Mando.Api.Enums.DashboardQueryStatus.Success => Ok(result.Data),
            _ => new ActionResult<T>(Problem("Unexpected dashboard query result."))
        };
    }
}