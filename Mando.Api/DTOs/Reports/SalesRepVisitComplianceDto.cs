namespace Mando.Api.DTOs.Reports;

public class SalesRepVisitComplianceDto
{
    public Guid SalesRepId { get; set; }
    public string SalesRepName { get; set; } = string.Empty;

    public int TotalAttempts { get; set; }
    public int SuccessfulAttempts { get; set; }
    public int FailedAttempts { get; set; }

    public int OutOfRangeAttempts { get; set; }
    public int WeakAccuracyAttempts { get; set; }

    public double AverageDistanceFromCustomerInMeters { get; set; }
}