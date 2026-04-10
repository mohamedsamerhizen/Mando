namespace Mando.Api.IntegrationTests.Contracts.Common;

public class ApiErrorResponseDto
{
    public bool Success { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
    public string TraceId { get; set; } = string.Empty;
}
