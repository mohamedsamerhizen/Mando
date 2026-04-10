namespace Mando.Api.DTOs.Visits;

public class VisitSalesRepPerformanceDto
{
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;

    public int StartedCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }

    public int SuccessfulVisitsCount { get; set; }

    public int VisitsWithOrdersCount { get; set; }
    public int VisitsWithPaymentsCount { get; set; }

    public double? AverageCompletedVisitDurationHours { get; set; }
}