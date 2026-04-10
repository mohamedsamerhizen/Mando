using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Visits;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Visits;

namespace Mando.Api.Interfaces.Visits;

public interface IVisitQueryService
{
    Task<VisitQueryResult<PagedResultDto<VisitResponseDto>>> GetAllAsync(
        GetVisitsQueryDto query,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<VisitQueryResult<VisitResponseDto>> GetByIdAsync(
        Guid visitId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<VisitQueryResult<IReadOnlyList<VisitActionHistoryResponseDto>>> GetHistoryAsync(
        Guid visitId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<VisitQueryResult<VisitTimelineResponseDto>> GetTimelineAsync(
        Guid visitId,
        string baseUrl,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<VisitQueryResult<VisitOperationsReportResponseDto>> GetOperationsReportAsync(
        GetVisitOperationsReportQueryDto query);
}