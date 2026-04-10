using System.ComponentModel.DataAnnotations;
using Mando.Api.DTOs.Common;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Visits;

public class GetVisitsQueryDto : PagedQueryDto
{
    public Guid? CustomerId { get; set; }

    [EnumDataType(typeof(VisitStatus))]
    public VisitStatus? Status { get; set; }

    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }
}