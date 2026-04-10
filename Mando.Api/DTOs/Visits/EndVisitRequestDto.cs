using System.ComponentModel.DataAnnotations;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Visits;

public class EndVisitRequestDto
{
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    [Range(0, 10000)]
    public decimal AccuracyInMeters { get; set; }

    [EnumDataType(typeof(VisitOutcome))]
    public VisitOutcome Outcome { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}