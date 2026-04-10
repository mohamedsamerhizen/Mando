namespace Mando.Api.Interfaces.Visits;

public interface IVisitLifecycleLockService
{
    Task<bool> LockAsync(Guid visitId, CancellationToken cancellationToken = default);
}
