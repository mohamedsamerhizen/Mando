using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mando.Api.Common;
using Mando.Api.Data;
using Mando.Api.DTOs.Visits;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Visits;
using Mando.Api.Models.Visits;

namespace Mando.Api.Services.Visits;

public class VisitMediaService : IVisitMediaService
{
    private const long MaxImageSizeInBytes = 5 * 1024 * 1024;
    private const int MaxImagesPerVisit = 5;
    private const int SignatureProbeLength = 12;

    private readonly AppDbContext _context;
    private readonly IVisitImageStorage _visitImageStorage;
    private readonly IWorkflowSideEffectService _workflowSideEffectService;

    public VisitMediaService(
        AppDbContext context,
        IVisitImageStorage visitImageStorage,
        IWorkflowSideEffectService workflowSideEffectService)
    {
        _context = context;
        _visitImageStorage = visitImageStorage;
        _workflowSideEffectService = workflowSideEffectService;
    }

    public async Task<VisitMediaResult<VisitImageResponseDto>> UploadImageAsync(
        Guid visitId,
        IFormFile? file,
        string baseUrl,
        AppUser currentUser)
    {
        if (file is null || file.Length == 0)
            return new VisitMediaResult<VisitImageResponseDto> { Status = VisitMediaStatus.ImageFileRequired };

        if (file.Length > MaxImageSizeInBytes)
            return new VisitMediaResult<VisitImageResponseDto> { Status = VisitMediaStatus.ImageSizeExceeded };

        await using var inputStream = file.OpenReadStream();
        var detectedImageFormat = await DetectImageFormatAsync(inputStream);
        if (detectedImageFormat is null)
            return new VisitMediaResult<VisitImageResponseDto> { Status = VisitMediaStatus.InvalidImageType };

        if (IsClaimedContentTypeConflicting(file.ContentType, detectedImageFormat.ContentType))
            return new VisitMediaResult<VisitImageResponseDto> { Status = VisitMediaStatus.InvalidImageType };

        if (inputStream.CanSeek)
            inputStream.Position = 0;

        string? stagedFilePath = null;
        string? finalPhysicalPath = null;
        VisitImage? visitImage = null;

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await AcquireVisitRowLockAsync(visitId);

                var visit = await _context.Visits
                    .FirstOrDefaultAsync(x => x.Id == visitId);

                if (visit is null)
                {
                    await transaction.RollbackAsync();
                    return new VisitMediaResult<VisitImageResponseDto> { Status = VisitMediaStatus.VisitNotFound };
                }

                if (visit.SalesRepId != currentUser.Id)
                {
                    await transaction.RollbackAsync();
                    return new VisitMediaResult<VisitImageResponseDto> { Status = VisitMediaStatus.Forbidden };
                }

                if (visit.Status != VisitStatus.InProgress)
                {
                    await transaction.RollbackAsync();
                    return new VisitMediaResult<VisitImageResponseDto> { Status = VisitMediaStatus.VisitNotInProgress };
                }

                var takenSlots = await _context.VisitImages
                    .Where(x => x.VisitId == visitId)
                    .Select(x => x.SlotNumber)
                    .ToListAsync();

                var slotNumber = Enumerable.Range(1, MaxImagesPerVisit)
                    .FirstOrDefault(slot => !takenSlots.Contains(slot));

                if (slotNumber == 0)
                {
                    await transaction.RollbackAsync();
                    return new VisitMediaResult<VisitImageResponseDto> { Status = VisitMediaStatus.MaxVisitImagesReached };
                }

                stagedFilePath = await _visitImageStorage.StageAsync(
                    visitId,
                    detectedImageFormat.Extension,
                    inputStream);

                var storedFileName = $"{Guid.NewGuid():N}{detectedImageFormat.Extension}";
                var relativePath = _visitImageStorage.BuildStoredRelativePath(visitId, storedFileName);

                visitImage = new VisitImage
                {
                    Id = Guid.NewGuid(),
                    VisitId = visitId,
                    UploadedByUserId = currentUser.Id,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    StoredFileName = storedFileName,
                    RelativePath = relativePath,
                    ContentType = detectedImageFormat.ContentType,
                    FileSizeInBytes = file.Length,
                    SlotNumber = slotNumber,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.VisitImages.Add(visitImage);
                await _context.SaveChangesAsync();

                finalPhysicalPath = _visitImageStorage.MoveStagedToStored(stagedFilePath, relativePath);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

                if (!string.IsNullOrWhiteSpace(finalPhysicalPath) && File.Exists(finalPhysicalPath))
                    _visitImageStorage.DeleteIfExists(finalPhysicalPath);

                throw;
            }
        }
        finally
        {
            _visitImageStorage.DeleteIfExists(stagedFilePath);
        }

        await _workflowSideEffectService.WriteAuditAsync(
            currentUser.Id,
            AuditActionType.VisitImageUploaded,
            nameof(VisitImage),
            visitImage!.Id,
            $"Visit image '{visitImage.OriginalFileName}' was uploaded to visit '{visitImage.VisitId}' by '{currentUser.FullName}' in slot {visitImage.SlotNumber}.");

        return new VisitMediaResult<VisitImageResponseDto>
        {
            Status = VisitMediaStatus.Success,
            Data = MapVisitImage(visitImage, currentUser.FullName, baseUrl)
        };
    }

    public async Task<VisitMediaResult<List<VisitImageResponseDto>>> GetImagesAsync(
        Guid visitId,
        string baseUrl,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var visit = await _context.Visits
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == visitId);

        if (visit is null)
            return new VisitMediaResult<List<VisitImageResponseDto>> { Status = VisitMediaStatus.VisitNotFound };

        var isAdminOrManager = HasPrivilegedAccess(currentUserRoles);
        if (!isAdminOrManager && visit.SalesRepId != currentUser.Id)
            return new VisitMediaResult<List<VisitImageResponseDto>> { Status = VisitMediaStatus.Forbidden };

        var images = await _context.VisitImages
            .Include(x => x.UploadedByUser)
            .Where(x => x.VisitId == visitId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync();

        return new VisitMediaResult<List<VisitImageResponseDto>>
        {
            Status = VisitMediaStatus.Success,
            Data = images
                .Select(x => MapVisitImage(x, x.UploadedByUser.FullName, baseUrl))
                .ToList()
        };
    }

    public async Task<VisitMediaResult<VisitImageContentPayload>> GetImageContentAsync(
        Guid imageId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var image = await _context.VisitImages
            .Include(x => x.Visit)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == imageId);

        if (image is null)
            return new VisitMediaResult<VisitImageContentPayload> { Status = VisitMediaStatus.VisitImageNotFound };

        var isAdminOrManager = HasPrivilegedAccess(currentUserRoles);
        if (!isAdminOrManager && image.Visit.SalesRepId != currentUser.Id)
            return new VisitMediaResult<VisitImageContentPayload> { Status = VisitMediaStatus.Forbidden };

        var physicalPath = _visitImageStorage.ResolveStoredPath(image.RelativePath);
        if (!File.Exists(physicalPath))
            return new VisitMediaResult<VisitImageContentPayload> { Status = VisitMediaStatus.VisitImageNotFound };

        return new VisitMediaResult<VisitImageContentPayload>
        {
            Status = VisitMediaStatus.Success,
            Data = new VisitImageContentPayload
            {
                PhysicalPath = physicalPath,
                ContentType = image.ContentType,
                OriginalFileName = image.OriginalFileName
            }
        };
    }

    public async Task<VisitMediaResult<bool>> DeleteImageAsync(
        Guid imageId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        string? originalPhysicalPath = null;
        string? recycledFilePath = null;
        string? originalRelativePath = null;
        Guid visitId = Guid.Empty;
        string originalFileName = string.Empty;
        var originalFileExists = false;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var image = await _context.VisitImages
                .Include(x => x.Visit)
                .FirstOrDefaultAsync(x => x.Id == imageId);

            if (image is null)
            {
                await transaction.RollbackAsync();
                return new VisitMediaResult<bool> { Status = VisitMediaStatus.VisitImageNotFound };
            }

            var isAdminOrManager = HasPrivilegedAccess(currentUserRoles);
            if (!isAdminOrManager && image.Visit.SalesRepId != currentUser.Id)
            {
                await transaction.RollbackAsync();
                return new VisitMediaResult<bool> { Status = VisitMediaStatus.Forbidden };
            }

            await AcquireVisitRowLockAsync(image.VisitId);

            image = await _context.VisitImages
                .Include(x => x.Visit)
                .FirstOrDefaultAsync(x => x.Id == imageId);

            if (image is null)
            {
                await transaction.RollbackAsync();
                return new VisitMediaResult<bool> { Status = VisitMediaStatus.VisitImageNotFound };
            }

            if (!isAdminOrManager && image.Visit.SalesRepId != currentUser.Id)
            {
                await transaction.RollbackAsync();
                return new VisitMediaResult<bool> { Status = VisitMediaStatus.Forbidden };
            }

            if (image.Visit.Status != VisitStatus.InProgress)
            {
                await transaction.RollbackAsync();
                return new VisitMediaResult<bool> { Status = VisitMediaStatus.VisitNotInProgress };
            }

            visitId = image.VisitId;
            originalFileName = image.OriginalFileName;
            originalRelativePath = image.RelativePath;
            originalPhysicalPath = _visitImageStorage.ResolveStoredPath(image.RelativePath);
            originalFileExists = File.Exists(originalPhysicalPath);

            if (originalFileExists)
            {
                recycledFilePath = _visitImageStorage.MoveStoredToRecycle(
                    originalPhysicalPath,
                    image.VisitId,
                    Path.GetExtension(image.StoredFileName));
            }

            _context.VisitImages.Remove(image);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            if (!string.IsNullOrWhiteSpace(recycledFilePath) &&
                !string.IsNullOrWhiteSpace(originalPhysicalPath) &&
                File.Exists(recycledFilePath))
            {
                _visitImageStorage.RestoreFromRecycle(recycledFilePath, originalPhysicalPath);
            }

            throw;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(recycledFilePath) && File.Exists(recycledFilePath))
                _visitImageStorage.DeleteIfExists(recycledFilePath);
        }

        if (originalFileExists && !string.IsNullOrWhiteSpace(originalPhysicalPath))
        {
            var visitFolderPath = Path.GetDirectoryName(originalPhysicalPath);
            _visitImageStorage.CleanupEmptyDirectories(visitFolderPath, originalRelativePath);
        }

        await _workflowSideEffectService.WriteAuditAsync(
            currentUser.Id,
            AuditActionType.VisitImageDeleted,
            nameof(VisitImage),
            imageId,
            $"Visit image '{originalFileName}' was deleted from visit '{visitId}' by '{currentUser.FullName}'.");

        return new VisitMediaResult<bool>
        {
            Status = VisitMediaStatus.Success,
            Data = true
        };
    }

    private async Task AcquireVisitRowLockAsync(Guid visitId)
    {
        if (!_context.Database.IsSqlServer())
        {
            _ = await _context.Visits
                .AsNoTracking()
                .AnyAsync(x => x.Id == visitId);

            return;
        }

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $@"SELECT TOP (1) 1
               FROM Visits WITH (UPDLOCK, HOLDLOCK)
               WHERE Id = {visitId}");
    }

    private static bool HasPrivilegedAccess(IEnumerable<string> currentUserRoles)
    {
        return currentUserRoles.Contains(AppRoles.Admin) || currentUserRoles.Contains(AppRoles.Manager);
    }

    private static string BuildAuthorizedImageUrl(string baseUrl, Guid imageId)
    {
        return $"{baseUrl.TrimEnd('/')}/api/visits/images/{imageId}/content";
    }

    private VisitImageResponseDto MapVisitImage(VisitImage image, string uploadedByUserName, string baseUrl)
    {
        return new VisitImageResponseDto
        {
            Id = image.Id,
            VisitId = image.VisitId,
            OriginalFileName = image.OriginalFileName,
            ContentType = image.ContentType,
            FileSizeInBytes = image.FileSizeInBytes,
            ImageUrl = BuildAuthorizedImageUrl(baseUrl, image.Id),
            UploadedByUserId = image.UploadedByUserId,
            UploadedByUserName = uploadedByUserName,
            CreatedAtUtc = image.CreatedAtUtc
        };
    }

    private static async Task<DetectedImageFormat?> DetectImageFormatAsync(Stream inputStream)
    {
        var buffer = new byte[SignatureProbeLength];
        var bytesRead = await inputStream.ReadAsync(buffer.AsMemory(0, buffer.Length));

        if (bytesRead < 4)
            return null;

        if (IsJpeg(buffer, bytesRead))
            return new DetectedImageFormat("image/jpeg", ".jpg");

        if (IsPng(buffer, bytesRead))
            return new DetectedImageFormat("image/png", ".png");

        if (IsWebp(buffer, bytesRead))
            return new DetectedImageFormat("image/webp", ".webp");

        return null;
    }

    private static bool IsClaimedContentTypeConflicting(string? claimedContentType, string detectedContentType)
    {
        if (string.IsNullOrWhiteSpace(claimedContentType))
            return false;

        var normalizedClaimedContentType = claimedContentType.Trim().ToLowerInvariant();

        return normalizedClaimedContentType != "application/octet-stream" &&
               normalizedClaimedContentType != detectedContentType;
    }

    private static bool IsJpeg(byte[] buffer, int bytesRead)
    {
        return bytesRead >= 3 &&
               buffer[0] == 0xFF &&
               buffer[1] == 0xD8 &&
               buffer[2] == 0xFF;
    }

    private static bool IsPng(byte[] buffer, int bytesRead)
    {
        return bytesRead >= 8 &&
               buffer[0] == 0x89 &&
               buffer[1] == 0x50 &&
               buffer[2] == 0x4E &&
               buffer[3] == 0x47 &&
               buffer[4] == 0x0D &&
               buffer[5] == 0x0A &&
               buffer[6] == 0x1A &&
               buffer[7] == 0x0A;
    }

    private static bool IsWebp(byte[] buffer, int bytesRead)
    {
        return bytesRead >= 12 &&
               buffer[0] == 0x52 &&
               buffer[1] == 0x49 &&
               buffer[2] == 0x46 &&
               buffer[3] == 0x46 &&
               buffer[8] == 0x57 &&
               buffer[9] == 0x45 &&
               buffer[10] == 0x42 &&
               buffer[11] == 0x50;
    }

    private sealed record DetectedImageFormat(string ContentType, string Extension);
}
