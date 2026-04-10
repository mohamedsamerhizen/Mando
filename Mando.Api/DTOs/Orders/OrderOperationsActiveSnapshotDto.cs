namespace Mando.Api.DTOs.Orders;

public class OrderOperationsActiveSnapshotDto
{
    public int ActiveOrdersCount { get; set; }
    public decimal ActiveOrdersAmount { get; set; }

    public int CustomersWithActiveOrdersCount { get; set; }
    public int SalesRepsWithActiveOrdersCount { get; set; }

    public int StaleActiveOrdersCount { get; set; }
    public decimal StaleActiveOrdersAmount { get; set; }

    public double? AverageActiveOrderAgeInHours { get; set; }
    public double? OldestActiveOrderAgeInHours { get; set; }
}