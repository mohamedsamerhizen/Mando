using Mando.Api.DTOs.Audit;
using Mando.Api.DTOs.Common;
using Mando.Api.Models.Audit;

namespace Mando.Api.Interfaces.Audit;

public interface IAuditLogQueryService
{
    Task<AuditLogQueryResult<PagedResultDto<AuditLogResponseDto>>> GetAllAsync(GetAuditLogsQueryDto query);
}