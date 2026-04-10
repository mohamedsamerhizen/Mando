namespace Mando.Api.DTOs.Visits;

public class VisitImageResponseDto
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeInBytes { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public Guid UploadedByUserId { get; set; }
    public string UploadedByUserName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}