using Mando.Api.Enums;

namespace Mando.Api.DTOs.Visits;

public class VisitOutcomeBreakdownDto
{
    public VisitOutcome Outcome { get; set; }
    public int Count { get; set; }
}