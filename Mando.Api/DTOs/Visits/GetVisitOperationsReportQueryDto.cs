using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Visits;

public class GetVisitOperationsReportQueryDto
{
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }

    public Guid? SalesRepId { get; set; }
    public Guid? CustomerId { get; set; }

    [Range(1, 24 * 14)]
    public int StaleAfterHours { get; set; } = 8;
}