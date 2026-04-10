using Mando.Api.Enums;

namespace Mando.Api.DTOs.Dashboard;

public class DashboardRecentPaymentDto
{
    public Guid PaymentId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string SalesRepName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}