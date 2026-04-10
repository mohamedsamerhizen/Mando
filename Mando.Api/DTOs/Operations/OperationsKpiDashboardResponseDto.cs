namespace Mando.Api.DTOs.Operations;

public class OperationsKpiDashboardResponseDto
{
    public DateTime DateFromUtc { get; set; }
    public DateTime DateToUtc { get; set; }
    public int TopCount { get; set; }

    public List<TopSalesRepByVisitsDto> TopSalesRepsByVisits { get; set; } = [];
    public List<TopSalesRepBySalesDto> TopSalesRepsBySales { get; set; } = [];
    public List<TopSalesRepByCollectionsDto> TopSalesRepsByCollections { get; set; } = [];
    public List<TopCustomerActivityDto> TopCustomersByActivity { get; set; } = [];
    public List<TopCustomerDebtDto> TopCustomersByDebt { get; set; } = [];
}