using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class VisitActionHistory : BaseEntity
{
    public Guid VisitId { get; set; }
    public Visit Visit { get; set; } = default!;

    public VisitActionType ActionType { get; set; }

    public VisitStatus? PreviousStatus { get; set; }
    public VisitStatus NewStatus { get; set; }

    public VisitOutcome? PreviousOutcome { get; set; }
    public VisitOutcome NewOutcome { get; set; }

    public Guid PerformedByUserId { get; set; }
    public AppUser PerformedByUser { get; set; } = default!;

    public string PerformedByUserFullName { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public DateTime ActionAtUtc { get; set; } = DateTime.UtcNow;
}