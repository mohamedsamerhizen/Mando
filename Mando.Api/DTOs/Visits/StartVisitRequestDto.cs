using System.ComponentModel.DataAnnotations;

namespace Mando.Api.DTOs.Visits;

public class StartVisitRequestDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    [Range(0, 10000)]
    public decimal AccuracyInMeters { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}