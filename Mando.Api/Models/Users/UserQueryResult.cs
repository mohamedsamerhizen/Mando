using Mando.Api.Enums;

namespace Mando.Api.Models.Users;

public sealed class UserQueryResult<T>
{
    public UserQueryStatus Status { get; init; }
    public T? Data { get; init; }
}