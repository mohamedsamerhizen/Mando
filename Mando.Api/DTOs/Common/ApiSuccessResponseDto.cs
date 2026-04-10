namespace Mando.Api.DTOs.Common;

public class ApiSuccessResponseDto<T>
{
    public bool Success => true;
    public string Code { get; set; } = "success";
    public string Message { get; set; } = "Request completed successfully.";
    public T? Data { get; set; }
    public string TraceId { get; set; } = string.Empty;
}