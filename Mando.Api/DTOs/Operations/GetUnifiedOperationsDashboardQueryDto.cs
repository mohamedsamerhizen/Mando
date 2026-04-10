using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Operations;

public class GetUnifiedOperationsDashboardQueryDto
{
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }

    public Guid? SalesRepId { get; set; }
    public Guid? CustomerId { get; set; }

    [Range(1, 24 * 30)]
    public int PaymentStaleAfterHours { get; set; } = 24;

    [Range(1, 24 * 30)]
    public int OrderStaleAfterHours { get; set; } = 24;

    [Range(1, 24 * 14)]
    public int VisitStaleAfterHours { get; set; } = 8;
}