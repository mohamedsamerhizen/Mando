using Mando.Api.Enums;

namespace Mando.Api.DTOs.Operations;

public class OperationVisitSummaryDto
{
    public Guid VisitId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;
    public DateTime CheckInAtUtc { get; set; }
    public DateTime? CheckOutAtUtc { get; set; }
    public VisitStatus Status { get; set; }
    public VisitOutcome? Outcome { get; set; }
}