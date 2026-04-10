using Mando.Api.DTOs.Dashboard;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Dashboard;

namespace Mando.Api.Interfaces.Dashboard;

public interface IDashboardQueryService
{
    Task<DashboardQueryResult<DashboardSummaryDto>> GetAdminSummaryAsync();

    Task<DashboardQueryResult<SalesRepDashboardSummaryDto>> GetSalesRepSummaryAsync(AppUser currentUser);
}