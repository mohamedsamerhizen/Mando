using Mando.Api.DTOs.Reports;

namespace Mando.Api.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }

    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }

    public int TotalVisits { get; set; }
    public int InProgressVisits { get; set; }

    public int TotalOrders { get; set; }
    public decimal TotalOrderAmount { get; set; }

    public int TotalPayments { get; set; }
    public decimal ApprovedPaymentsTotal { get; set; }

    public int PendingPaymentsCount { get; set; }

    public List<DashboardRecentVisitDto> RecentVisits { get; set; } = [];
    public List<DashboardRecentOrderDto> RecentOrders { get; set; } = [];
    public List<DashboardRecentPaymentDto> RecentPayments { get; set; } = [];
    public List<DashboardRecentPaymentDto> PendingPayments { get; set; } = [];
    public List<CustomerBalanceReportDto> TopDebtCustomers { get; set; } = [];
}