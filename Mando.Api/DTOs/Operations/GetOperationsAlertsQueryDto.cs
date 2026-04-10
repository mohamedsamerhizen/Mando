using System.ComponentModel.DataAnnotations;
using Mando.Api.DTOs.Common;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Operations;

public class GetOperationsAlertsQueryDto : PagedQueryDto
{
    public Guid? SalesRepId { get; set; }
    public Guid? CustomerId { get; set; }

    [EnumDataType(typeof(OperationsAlertSeverity))]
    public OperationsAlertSeverity? Severity { get; set; }

    [EnumDataType(typeof(OperationsAlertCategory))]
    public OperationsAlertCategory? Category { get; set; }

    [EnumDataType(typeof(OperationsAlertEntityType))]
    public OperationsAlertEntityType? EntityType { get; set; }

    [Range(1, 24 * 30)]
    public int PaymentStaleAfterHours { get; set; } = 24;

    [Range(1, 24 * 30)]
    public int OrderStaleAfterHours { get; set; } = 24;

    [Range(1, 24 * 14)]
    public int VisitStaleAfterHours { get; set; } = 8;

    [Range(typeof(decimal), "0.50", "1.00")]
    public decimal NearCreditLimitRatio { get; set; } = 0.90m;

    public bool IncludeNearCreditLimitAlerts { get; set; } = true;
}