using Mando.Api.Common;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;

namespace Mando.Api.Entities;

public class Notification : AuditableEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = default!;

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAtUtc { get; set; }

    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
}