using Mando.Api.Enums;

namespace Mando.Api.Models.Notifications;

public sealed class NotificationQueryResult<T>
{
    public NotificationQueryStatus Status { get; init; }
    public T? Data { get; init; }
}