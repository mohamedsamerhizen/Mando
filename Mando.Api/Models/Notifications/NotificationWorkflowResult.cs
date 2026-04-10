using Mando.Api.Enums;

namespace Mando.Api.Models.Notifications;

public sealed class NotificationWorkflowResult<T>
{
    public NotificationWorkflowStatus Status { get; init; }
    public T? Data { get; init; }
}