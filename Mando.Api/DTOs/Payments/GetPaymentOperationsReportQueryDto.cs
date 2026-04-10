using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Payments;

public class GetPaymentOperationsReportQueryDto
{
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }

    public Guid? SalesRepId { get; set; }
    public Guid? CustomerId { get; set; }

    [Range(1, 24 * 30)]
    public int StaleAfterHours { get; set; } = 24;
}