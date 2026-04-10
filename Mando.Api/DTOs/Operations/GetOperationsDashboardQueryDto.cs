using System.ComponentModel.DataAnnotations;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Operations;

public class GetOperationsDashboardQueryDto
{
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }

    public Guid? SalesRepId { get; set; }
    public Guid? CustomerId { get; set; }

    [EnumDataType(typeof(VisitStatus))]
    public VisitStatus? VisitStatus { get; set; }

    [EnumDataType(typeof(PaymentStatus))]
    public PaymentStatus? PaymentStatus { get; set; }

    public bool IncludeVisits { get; set; } = true;
    public bool IncludeOrders { get; set; } = true;
    public bool IncludePayments { get; set; } = true;

    [Range(1, 500)]
    public int ItemsLimit { get; set; } = 50;
}