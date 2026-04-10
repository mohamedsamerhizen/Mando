using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Mando.Api.Middleware;

public class RequestLoggingMiddleware
{
    private const int MaxLoggedUserAgentLength = 256;

    private static readonly EventId RequestCompletedEventId = new(1000, nameof(RequestCompletedEventId));
    private static readonly EventId RequestFailedEventId = new(1001, nameof(RequestFailedEventId));

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestStartedAtUtc = DateTime.UtcNow;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? string.Empty;
            var queryParameterCount = context.Request.Query.Count;
            var statusCode = context.Response.StatusCode;
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            var traceId = context.TraceIdentifier;
            var userId = ResolveUserId(context.User);
            var remoteIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = NormalizeUserAgent(context.Request.Headers.UserAgent.ToString());
            var contentLength = context.Request.ContentLength;
            var contentType = context.Request.ContentType;
            var requestProtocol = context.Request.Protocol;

            const string message =
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms | TraceId: {TraceId} | UserId: {UserId} | RemoteIp: {RemoteIp} | QueryParameterCount: {QueryParameterCount} | ContentLength: {ContentLength} | ContentType: {ContentType} | Protocol: {Protocol} | StartedAtUtc: {StartedAtUtc} | UserAgent: {UserAgent}";

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    RequestFailedEventId,
                    message,
                    method,
                    path,
                    statusCode,
                    elapsedMilliseconds,
                    traceId,
                    userId,
                    remoteIpAddress,
                    queryParameterCount,
                    contentLength,
                    contentType,
                    requestProtocol,
                    requestStartedAtUtc,
                    userAgent);
            }
            else if (statusCode >= StatusCodes.Status400BadRequest)
            {
                _logger.LogWarning(
                    RequestCompletedEventId,
                    message,
                    method,
                    path,
                    statusCode,
                    elapsedMilliseconds,
                    traceId,
                    userId,
                    remoteIpAddress,
                    queryParameterCount,
                    contentLength,
                    contentType,
                    requestProtocol,
                    requestStartedAtUtc,
                    userAgent);
            }
            else if (IsNoiseLevelEndpoint(path))
            {
                _logger.LogDebug(
                    RequestCompletedEventId,
                    message,
                    method,
                    path,
                    statusCode,
                    elapsedMilliseconds,
                    traceId,
                    userId,
                    remoteIpAddress,
                    queryParameterCount,
                    contentLength,
                    contentType,
                    requestProtocol,
                    requestStartedAtUtc,
                    userAgent);
            }
            else
            {
                _logger.LogInformation(
                    RequestCompletedEventId,
                    message,
                    method,
                    path,
                    statusCode,
                    elapsedMilliseconds,
                    traceId,
                    userId,
                    remoteIpAddress,
                    queryParameterCount,
                    contentLength,
                    contentType,
                    requestProtocol,
                    requestStartedAtUtc,
                    userAgent);
            }
        }
    }

    private static bool IsNoiseLevelEndpoint(string path)
    {
        return path.Equals("/health/live", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/health/ready", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "unknown";

        var sanitizedUserAgent = new string(userAgent
            .Where(character => !char.IsControl(character))
            .ToArray())
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitizedUserAgent))
            return "unknown";

        return sanitizedUserAgent.Length <= MaxLoggedUserAgentLength
            ? sanitizedUserAgent
            : sanitizedUserAgent[..MaxLoggedUserAgentLength];
    }

    private static string ResolveUserId(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return "anonymous";

        return user.FindFirst("sub")?.Value
               ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? "authenticated";
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
