using System.ComponentModel.DataAnnotations;

namespace Mando.Api.Configurations;

public class GpsSettings
{
    public const string SectionName = "Gps";

    [Range(1, 1000)]
    public double MaxStartVisitDistanceMeters { get; set; } = 150;

    [Range(1, 1000)]
    public double MaxEndVisitDistanceMeters { get; set; } = 150;

    [Range(1, 1000)]
    public double MaxAllowedAccuracyMeters { get; set; } = 100;
}
