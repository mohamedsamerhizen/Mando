namespace Mando.Api.DTOs.Operations;

public class OperationsAlertsSummaryDto
{
    public int TotalCount { get; set; }

    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }

    public int PaymentAlertsCount { get; set; }
    public int OrderAlertsCount { get; set; }
    public int VisitAlertsCount { get; set; }
    public int CustomerAlertsCount { get; set; }

    public int OpenCount { get; set; }
    public int AcknowledgedCount { get; set; }
    public int ResolvedCount { get; set; }
    public int DismissedCount { get; set; }
}