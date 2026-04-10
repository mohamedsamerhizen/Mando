namespace Mando.Api.DTOs.Visits;

public class VisitOperationsActiveSnapshotDto
{
    public int InProgressCount { get; set; }
    public int StaleInProgressCount { get; set; }

    public int CustomersWithInProgressVisitsCount { get; set; }
    public int SalesRepsWithInProgressVisitsCount { get; set; }

    public double? AverageInProgressAgeInHours { get; set; }
    public double? OldestInProgressAgeInHours { get; set; }
}