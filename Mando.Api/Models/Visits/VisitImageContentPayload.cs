namespace Mando.Api.Models.Visits;

public sealed class VisitImageContentPayload
{
    public string PhysicalPath { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
}
