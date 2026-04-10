using Mando.Api.Enums;

namespace Mando.Api.DTOs.Visits;

public class VisitActionHistoryResponseDto
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }

    public VisitActionType ActionType { get; set; }

    public VisitStatus? PreviousStatus { get; set; }
    public VisitStatus NewStatus { get; set; }

    public VisitOutcome? PreviousOutcome { get; set; }
    public VisitOutcome NewOutcome { get; set; }

    public Guid PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public DateTime ActionAtUtc { get; set; }
}