namespace Mando.Api.DTOs.Dashboard;

public class DashboardRecentVisitDto
{
    public Guid VisitId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string SalesRepName { get; set; } = string.Empty;
    public DateTime CheckInAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
}