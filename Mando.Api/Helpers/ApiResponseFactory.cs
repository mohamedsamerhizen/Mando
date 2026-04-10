using Microsoft.AspNetCore.Mvc;
using Mando.Api.DTOs.Common;

namespace Mando.Api.Helpers;

public static class ApiResponseFactory
{
    public static ApiErrorResponseDto Build(
        HttpContext httpContext,
        string code,
        string message,
        Dictionary<string, string[]>? errors = null)
    {
        return new ApiErrorResponseDto
        {
            Code = code,
            Message = message,
            Errors = errors,
            TraceId = httpContext.TraceIdentifier
        };
    }

    public static ApiSuccessResponseDto<T> BuildSuccess<T>(
        HttpContext httpContext,
        T? data,
        string message = "Request completed successfully.",
        string code = "success")
    {
        return new ApiSuccessResponseDto<T>
        {
            Code = code,
            Message = message,
            Data = data,
            TraceId = httpContext.TraceIdentifier
        };
    }

    public static BadRequestObjectResult BadRequest(
        HttpContext httpContext,
        string code,
        string message,
        Dictionary<string, string[]>? errors = null)
    {
        return new BadRequestObjectResult(Build(httpContext, code, message, errors));
    }

    public static NotFoundObjectResult NotFound(
        HttpContext httpContext,
        string code,
        string message)
    {
        return new NotFoundObjectResult(Build(httpContext, code, message));
    }

    public static ObjectResult Conflict(
        HttpContext httpContext,
        string code,
        string message)
    {
        return new ObjectResult(Build(httpContext, code, message))
        {
            StatusCode = StatusCodes.Status409Conflict
        };
    }

    public static UnauthorizedObjectResult Unauthorized(
        HttpContext httpContext,
        string code,
        string message)
    {
        return new UnauthorizedObjectResult(Build(httpContext, code, message));
    }

    public static ObjectResult Forbidden(
        HttpContext httpContext,
        string code,
        string message)
    {
        return new ObjectResult(Build(httpContext, code, message))
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }

    public static BadRequestObjectResult BadRequest(
        ControllerBase controller,
        string code,
        string message)
    {
        return BadRequest(controller.HttpContext, code, message);
    }

    public static NotFoundObjectResult NotFound(
        ControllerBase controller,
        string code,
        string message)
    {
        return NotFound(controller.HttpContext, code, message);
    }

    public static ObjectResult Conflict(
        ControllerBase controller,
        string code,
        string message)
    {
        return Conflict(controller.HttpContext, code, message);
    }

    public static UnauthorizedObjectResult Unauthorized(
        ControllerBase controller,
        string code,
        string message)
    {
        return Unauthorized(controller.HttpContext, code, message);
    }

    public static ObjectResult Forbidden(
        ControllerBase controller,
        string code,
        string message)
    {
        return Forbidden(controller.HttpContext, code, message);
    }
}