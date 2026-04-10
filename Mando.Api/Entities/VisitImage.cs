using Mando.Api.Common;
using Mando.Api.Entities.Identity;

namespace Mando.Api.Entities;

public class VisitImage : AuditableEntity
{
    public Guid VisitId { get; set; }
    public Guid UploadedByUserId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeInBytes { get; set; }
    public int SlotNumber { get; set; }

    public Visit Visit { get; set; } = null!;
    public AppUser UploadedByUser { get; set; } = null!;
}
