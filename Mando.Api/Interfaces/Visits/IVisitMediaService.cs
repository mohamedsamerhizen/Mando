using Microsoft.AspNetCore.Http;
using Mando.Api.DTOs.Visits;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Visits;

namespace Mando.Api.Interfaces.Visits;

public interface IVisitMediaService
{
    Task<VisitMediaResult<VisitImageResponseDto>> UploadImageAsync(
        Guid visitId,
        IFormFile? file,
        string baseUrl,
        AppUser currentUser);

    Task<VisitMediaResult<List<VisitImageResponseDto>>> GetImagesAsync(
        Guid visitId,
        string baseUrl,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<VisitMediaResult<VisitImageContentPayload>> GetImageContentAsync(
        Guid imageId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<VisitMediaResult<bool>> DeleteImageAsync(
        Guid imageId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);
}
