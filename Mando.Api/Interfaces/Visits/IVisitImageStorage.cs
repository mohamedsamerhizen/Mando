using Mando.Api.Entities;

namespace Mando.Api.Interfaces.Visits;

public interface IVisitImageStorage
{
    Task<string> StageAsync(Guid visitId, string extension, Stream inputStream, CancellationToken cancellationToken = default);
    string BuildStoredRelativePath(Guid visitId, string storedFileName);
    string ResolveStoredPath(string relativePath);
    string MoveStagedToStored(string stagedFilePath, string relativePath);
    string MoveStoredToRecycle(string originalPhysicalPath, Guid visitId, string extension);
    void RestoreFromRecycle(string recycledPhysicalPath, string originalPhysicalPath);
    void DeleteIfExists(string? physicalPath);
    void CleanupEmptyDirectories(string? startDirectory, string relativePath);
    bool IsLegacyPublicRelativePath(string relativePath);
    void CleanupTransientWorkDirectories(TimeSpan olderThan);
}
