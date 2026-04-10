
using System.Text.RegularExpressions;

namespace Mando.Api.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private static readonly Regex InvalidCharactersRegex = new("[^a-zA-Z0-9\\-_.:]", RegexOptions.Compiled);

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestCorrelationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();
        var correlationId = NormalizeCorrelationId(requestCorrelationId);

        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = context.TraceIdentifier;

        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = correlationId,
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }

    private static string? NormalizeCorrelationId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > 128)
            trimmed = trimmed[..128];

        trimmed = InvalidCharactersRegex.Replace(trimmed, string.Empty);

        return string.IsNullOrWhiteSpace(trimmed)
            ? null
            : trimmed;
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}


