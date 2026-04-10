using System.ComponentModel.DataAnnotations;
using Mando.Api.Enums;

namespace Mando.Api.DTOs.Operations;

public class ReviewOperationsAlertRequestDto
{
    [Required]
    public string AlertFingerprint { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(OperationsAlertReviewStatus))]
    public OperationsAlertReviewStatus Status { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}