using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Mando.Api.Interfaces.Visits;

namespace Mando.Api.Services.Visits;

public class LocalVisitImageStorage : IVisitImageStorage
{
    private const string LegacyPublicUploadsFolderName = "uploads";
    private const string VisitImagesFolderName = "visit-images";
    private const string PrivateMediaRootFolderName = "visit-media";
    private const string PrivateStoreFolderName = "store";
    private const string InternalWorkFolderName = "work";
    private const string StagingFolderName = "staging";
    private const string RecycleFolderName = "recycle";

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalVisitImageStorage> _logger;

    public LocalVisitImageStorage(
        IWebHostEnvironment environment,
        ILogger<LocalVisitImageStorage> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> StageAsync(
        Guid visitId,
        string extension,
        Stream inputStream,
        CancellationToken cancellationToken = default)
    {
        var stagingDirectory = Path.Combine(GetWorkRootPath(), StagingFolderName, visitId.ToString());
        Directory.CreateDirectory(stagingDirectory);

        var stagedFilePath = Path.Combine(stagingDirectory, $"{Guid.NewGuid():N}{extension}");

        await using var outputStream = new FileStream(
            stagedFilePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        await inputStream.CopyToAsync(outputStream, cancellationToken);
        return stagedFilePath;
    }

    public string BuildStoredRelativePath(Guid visitId, string storedFileName)
    {
        return $"{VisitImagesFolderName}/{visitId}/{storedFileName}";
    }

    public string ResolveStoredPath(string relativePath)
    {
        if (IsLegacyPublicRelativePath(relativePath))
            return ResolveLegacyPublicVisitImagePath(relativePath);

        var privateRoot = Path.GetFullPath(GetPrivateVisitImagesRootPath());
        var candidatePath = Path.GetFullPath(
            Path.Combine(privateRoot, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString())));

        if (!candidatePath.StartsWith(privateRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Visit image path resolved outside the allowed private storage root.");

        return candidatePath;
    }

    public string MoveStagedToStored(string stagedFilePath, string relativePath)
    {
        var finalPhysicalPath = ResolveStoredPath(relativePath);
        var finalDirectory = Path.GetDirectoryName(finalPhysicalPath)
            ?? throw new InvalidOperationException("Could not resolve final visit image directory.");

        Directory.CreateDirectory(finalDirectory);
        File.Move(stagedFilePath, finalPhysicalPath, overwrite: false);
        return finalPhysicalPath;
    }

    public string MoveStoredToRecycle(string originalPhysicalPath, Guid visitId, string extension)
    {
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
        var recycleDirectory = Path.Combine(GetWorkRootPath(), RecycleFolderName, visitId.ToString());
        Directory.CreateDirectory(recycleDirectory);

        var recycledFilePath = Path.Combine(recycleDirectory, $"{Guid.NewGuid():N}{safeExtension}");
        File.Move(originalPhysicalPath, recycledFilePath, overwrite: false);
        return recycledFilePath;
    }

    public void RestoreFromRecycle(string recycledPhysicalPath, string originalPhysicalPath)
    {
        var originalDirectory = Path.GetDirectoryName(originalPhysicalPath)
            ?? throw new InvalidOperationException("Could not resolve original visit image directory.");

        Directory.CreateDirectory(originalDirectory);
        File.Move(recycledPhysicalPath, originalPhysicalPath, overwrite: false);
    }

    public void DeleteIfExists(string? physicalPath)
    {
        if (string.IsNullOrWhiteSpace(physicalPath))
            return;

        try
        {
            if (File.Exists(physicalPath))
                File.Delete(physicalPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete visit media file '{FilePath}'.", physicalPath);
        }
    }

    public void CleanupEmptyDirectories(string? startDirectory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
            return;

        var stopDirectory = IsLegacyPublicRelativePath(relativePath)
            ? GetLegacyPublicVisitImagesRootPath(ResolveWebRootPath())
            : GetPrivateVisitImagesRootPath();

        var normalizedStopDirectory = Path.GetFullPath(stopDirectory);
        var currentDirectory = new DirectoryInfo(Path.GetFullPath(startDirectory));

        while (currentDirectory.Exists &&
               !string.Equals(currentDirectory.FullName, normalizedStopDirectory, StringComparison.OrdinalIgnoreCase))
        {
            if (currentDirectory.EnumerateFileSystemInfos().Any())
                break;

            try
            {
                currentDirectory.Delete();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete empty visit media directory '{DirectoryPath}'.", currentDirectory.FullName);
                break;
            }

            currentDirectory = currentDirectory.Parent;
            if (currentDirectory is null)
                break;
        }
    }

    public bool IsLegacyPublicRelativePath(string relativePath)
    {
        return relativePath.StartsWith(
            $"{LegacyPublicUploadsFolderName}/{VisitImagesFolderName}/",
            StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveLegacyPublicVisitImagePath(string relativePath)
    {
        var webRootPath = ResolveWebRootPath();
        var publicRoot = Path.GetFullPath(GetLegacyPublicVisitImagesRootPath(webRootPath));
        var candidatePath = Path.GetFullPath(
            Path.Combine(webRootPath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString())));

        if (!candidatePath.StartsWith(publicRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Visit image path resolved outside the allowed legacy uploads root.");

        return candidatePath;
    }

    public void CleanupTransientWorkDirectories(TimeSpan olderThan)
    {
        var cutoffUtc = DateTime.UtcNow.Subtract(olderThan);
        CleanupTransientBranch(Path.Combine(GetWorkRootPath(), StagingFolderName), cutoffUtc);
        CleanupTransientBranch(Path.Combine(GetWorkRootPath(), RecycleFolderName), cutoffUtc);
    }

    private void CleanupTransientBranch(string branchRoot, DateTime cutoffUtc)
    {
        if (!Directory.Exists(branchRoot))
            return;

        foreach (var filePath in Directory.EnumerateFiles(branchRoot, "*", SearchOption.AllDirectories))
        {
            try
            {
                var lastWriteUtc = File.GetLastWriteTimeUtc(filePath);
                if (lastWriteUtc > cutoffUtc)
                    continue;

                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup transient visit media file '{FilePath}'.", filePath);
            }
        }

        foreach (var directoryPath in Directory.EnumerateDirectories(branchRoot, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length))
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    continue;

                if (Directory.EnumerateFileSystemEntries(directoryPath).Any())
                    continue;

                Directory.Delete(directoryPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup transient visit media directory '{DirectoryPath}'.", directoryPath);
            }
        }
    }

    private string ResolveWebRootPath()
    {
        return _environment.WebRootPath
               ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }

    private string GetPrivateVisitImagesRootPath()
    {
        var contentRootPath = _environment.ContentRootPath
                              ?? Directory.GetCurrentDirectory();

        return Path.Combine(contentRootPath, "App_Data", PrivateMediaRootFolderName, PrivateStoreFolderName);
    }

    private string GetWorkRootPath()
    {
        var contentRootPath = _environment.ContentRootPath
                              ?? Directory.GetCurrentDirectory();

        return Path.Combine(contentRootPath, "App_Data", PrivateMediaRootFolderName, InternalWorkFolderName);
    }

    private static string GetLegacyPublicVisitImagesRootPath(string webRootPath)
    {
        return Path.Combine(webRootPath, LegacyPublicUploadsFolderName, VisitImagesFolderName);
    }
}
