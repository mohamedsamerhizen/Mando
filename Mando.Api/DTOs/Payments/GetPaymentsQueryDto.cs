using System.ComponentModel.DataAnnotations;
using Mando.Api.DTOs.Common;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Payments;

public class GetPaymentsQueryDto : PagedQueryDto
{
    public Guid? CustomerId { get; set; }
    public Guid? SalesRepId { get; set; }
    public Guid? ReviewedByUserId { get; set; }

    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod? PaymentMethod { get; set; }

    [EnumDataType(typeof(PaymentStatus))]
    public PaymentStatus? Status { get; set; }

    public string? PaymentNumber { get; set; }
    public string? Reference { get; set; }
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }
}