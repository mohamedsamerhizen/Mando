using System.ComponentModel.DataAnnotations;
using Mando.Api.DTOs.Common;

namespace Mando.Api.DTOs.Payments;

public class GetPaymentReviewQueueQueryDto : PagedQueryDto
{
    public Guid? SalesRepId { get; set; }
    public Guid? CustomerId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? MinAmount { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? MaxAmount { get; set; }

    public DateTime? SubmittedFromUtc { get; set; }
    public DateTime? SubmittedToUtc { get; set; }

    [Range(1, 24 * 30)]
    public int StaleAfterHours { get; set; } = 24;

    public bool StaleOnly { get; set; }
}