using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Mando.Api.DTOs.Visits;

public sealed class UploadVisitImageRequest
{
    [Required]
    public IFormFile File { get; set; } = default!;
}