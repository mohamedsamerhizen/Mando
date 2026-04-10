using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Orders;

public class GetOrderOperationsReportQueryDto
{
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }

    public Guid? SalesRepId { get; set; }
    public Guid? CustomerId { get; set; }

    [Range(1, 365)]
    public int StaleAfterHours { get; set; } = 24;
}