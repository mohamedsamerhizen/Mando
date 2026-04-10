namespace Mando.Api.IntegrationTests.Contracts.Common;

public class ApiSuccessResponseDto<T>
{
    public bool Success { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string TraceId { get; set; } = string.Empty;
}
