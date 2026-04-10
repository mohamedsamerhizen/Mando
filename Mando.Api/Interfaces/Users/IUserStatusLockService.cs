namespace Mando.Api.Interfaces.Users;

public interface IUserStatusLockService
{
    Task<bool> LockAsync(Guid userId, CancellationToken cancellationToken = default);
}
