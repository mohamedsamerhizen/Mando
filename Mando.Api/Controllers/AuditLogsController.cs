using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.DTOs.Audit;
using Mando.Api.DTOs.Common;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Audit;
using Mando.Api.Models.Audit;
using Mando.Api.Common;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogQueryService _auditLogQueryService;

    public AuditLogsController(IAuditLogQueryService auditLogQueryService)
    {
        _auditLogQueryService = auditLogQueryService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AuditLogResponseDto>>> GetAll([FromQuery] GetAuditLogsQueryDto query)
    {
        var result = await _auditLogQueryService.GetAllAsync(query);
        return MapResult(result);
    }

    private ActionResult<T> MapResult<T>(AuditLogQueryResult<T> result)
    {
        return result.Status switch
        {
            Mando.Api.Enums.AuditLogQueryStatus.Success => Ok(result.Data),
            _ => new ActionResult<T>(Problem("Unexpected audit logs query result."))
        };
    }
}