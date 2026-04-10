namespace Mando.Api.DTOs.Dashboard;

public class SalesRepDashboardSummaryDto
{
    public int MyCustomersCount { get; set; }
    public int MyActiveCustomersCount { get; set; }

    public int MyVisitsCount { get; set; }
    public int MyInProgressVisitsCount { get; set; }

    public int MyOrdersCount { get; set; }
    public decimal MyTotalSales { get; set; }

    public int MyPaymentsCount { get; set; }
    public decimal MyApprovedPaymentsTotal { get; set; }

    public int MyPendingPaymentsCount { get; set; }

    public List<DashboardRecentVisitDto> RecentVisits { get; set; } = [];
    public List<DashboardRecentOrderDto> RecentOrders { get; set; } = [];
    public List<DashboardRecentPaymentDto> RecentPayments { get; set; } = [];
}