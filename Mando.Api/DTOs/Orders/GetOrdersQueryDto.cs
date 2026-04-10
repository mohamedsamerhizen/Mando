using System.ComponentModel.DataAnnotations;
using Mando.Api.DTOs.Common;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Orders;

public class GetOrdersQueryDto : PagedQueryDto
{
    public Guid? CustomerId { get; set; }
    public Guid? SalesRepId { get; set; }
    public Guid? VisitId { get; set; }

    [EnumDataType(typeof(PaymentType))]
    public PaymentType? PaymentType { get; set; }

    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus? Status { get; set; }

    public string? OrderNumber { get; set; }
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }
}