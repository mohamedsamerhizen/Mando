using Microsoft.EntityFrameworkCore;
using Mando.Api.DTOs.Audit;
using Mando.Api.DTOs.Common;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Audit;
using Mando.Api.Models.Audit;

namespace Mando.Api.Services.Audit;

public class AuditLogQueryService : IAuditLogQueryService
{
    private readonly Data.AppDbContext _context;

    public AuditLogQueryService(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLogQueryResult<PagedResultDto<AuditLogResponseDto>>> GetAllAsync(GetAuditLogsQueryDto query)
    {
        var auditQuery = _context.AuditLogs.AsQueryable();

        if (query.ActionType.HasValue)
        {
            auditQuery = auditQuery.Where(x => x.ActionType == query.ActionType.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            var entityName = query.EntityName.Trim();
            auditQuery = auditQuery.Where(x => x.EntityName == entityName);
        }

        if (query.UserId.HasValue)
        {
            auditQuery = auditQuery.Where(x => x.UserId == query.UserId.Value);
        }

        if (query.DateFromUtc.HasValue)
        {
            auditQuery = auditQuery.Where(x => x.CreatedAtUtc >= query.DateFromUtc.Value);
        }

        var normalizedDateToUtc = NormalizeCreatedToUtc(query.DateToUtc);

        if (normalizedDateToUtc.HasValue)
        {
            auditQuery = auditQuery.Where(x => x.CreatedAtUtc < normalizedDateToUtc.Value);
        }

        var projectedQuery = auditQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AuditLogResponseDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserFullName = x.UserFullName,
                UserEmail = x.UserEmail,
                ActionType = x.ActionType,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Description = x.Description,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .AsNoTracking();

        var result = await projectedQuery.ToPagedResultAsync(query.PageNumber, query.PageSize);

        return new AuditLogQueryResult<PagedResultDto<AuditLogResponseDto>>
        {
            Status = AuditLogQueryStatus.Success,
            Data = result
        };
    }


    private static DateTime? NormalizeCreatedToUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.TimeOfDay == TimeSpan.Zero
            ? value.Value.Date.AddDays(1)
            : value.Value;
    }

}