using System.ComponentModel.DataAnnotations;
using Mando.Api.DTOs.Common;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Audit;

public class GetAuditLogsQueryDto : PagedQueryDto
{
    [EnumDataType(typeof(AuditActionType))]
    public AuditActionType? ActionType { get; set; }

    public string? EntityName { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }
}