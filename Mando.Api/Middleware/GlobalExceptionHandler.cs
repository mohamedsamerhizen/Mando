
using System.Security.Claims;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Mando.Api.Helpers;

namespace Mando.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning(
                exception,
                "Response has already started. Global exception handler cannot write a formatted response. TraceId: {TraceId} | Method: {Method} | Path: {Path}",
                httpContext.TraceIdentifier,
                httpContext.Request.Method,
                httpContext.Request.Path.Value);

            return false;
        }

        var (statusCode, code, message, logLevel) = MapException(exception);

        LogException(
            logLevel,
            exception,
            httpContext,
            statusCode,
            code);

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponseFactory.Build(
            httpContext,
            code,
            message);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Code, string Message, LogLevel LogLevel) MapException(Exception exception)
    {
        return exception switch
        {
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "concurrency_conflict",
                "The resource was changed by another request. Refresh and retry.",
                LogLevel.Warning),

            DbUpdateException dbUpdateException when DbUpdateExceptionHelper.IsUniqueConstraintViolation(dbUpdateException) => (
                StatusCodes.Status409Conflict,
                "duplicate_resource",
                "The request conflicts with an existing resource.",
                LogLevel.Warning),

            DbUpdateException dbUpdateException when DbUpdateExceptionHelper.IsTransientSqlFailure(dbUpdateException) => (
                StatusCodes.Status503ServiceUnavailable,
                "database_temporarily_unavailable",
                "The database is temporarily unavailable. Please retry.",
                LogLevel.Error),

            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "forbidden",
                "You are not allowed to perform this action.",
                LogLevel.Warning),

            OperationCanceledException => (
                StatusCodes.Status408RequestTimeout,
                "request_cancelled",
                "The request was cancelled before completion.",
                LogLevel.Information),

            _ => (
                StatusCodes.Status500InternalServerError,
                "server_error",
                "An unexpected error occurred.",
                LogLevel.Error)
        };
    }

    private void LogException(
        LogLevel logLevel,
        Exception exception,
        HttpContext httpContext,
        int statusCode,
        string code)
    {
        var traceId = httpContext.TraceIdentifier;
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var queryParameterCount = httpContext.Request.Query.Count;
        var userId = httpContext.User?.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "authenticated"
            : "anonymous";

        const string message =
            "Unhandled exception mapped to HTTP {StatusCode} with code '{Code}'. TraceId: {TraceId} | Method: {Method} | Path: {Path} | QueryParameterCount: {QueryParameterCount} | UserId: {UserId}";

        switch (logLevel)
        {
            case LogLevel.Information:
                _logger.LogInformation(
                    exception,
                    message,
                    statusCode,
                    code,
                    traceId,
                    method,
                    path,
                    queryParameterCount,
                    userId);
                break;

            case LogLevel.Warning:
                _logger.LogWarning(
                    exception,
                    message,
                    statusCode,
                    code,
                    traceId,
                    method,
                    path,
                    queryParameterCount,
                    userId);
                break;

            default:
                _logger.LogError(
                    exception,
                    message,
                    statusCode,
                    code,
                    traceId,
                    method,
                    path,
                    queryParameterCount,
                    userId);
                break;
        }
    }
}


