using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Mando.Api.DTOs.Common;
using Mando.Api.Helpers;

namespace Mando.Api.Filters;

public class ApiResponseEnvelopeFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        context.Result = WrapResult(context, context.Result);
        await next();
    }

    private static IActionResult WrapResult(
        ResultExecutingContext context,
        IActionResult result)
    {
        return result switch
        {
            FileResult => result,
            ChallengeResult => result,
            SignInResult => result,
            SignOutResult => result,
            RedirectResult => result,
            RedirectToActionResult => result,
            RedirectToRouteResult => result,
            RedirectToPageResult => result,

            NoContentResult => new OkObjectResult(
                ApiResponseFactory.BuildSuccess<object?>(
                    context.HttpContext,
                    null,
                    "Request completed successfully.")),

            UnauthorizedResult => ApiResponseFactory.Unauthorized(
                context.HttpContext,
                "unauthorized",
                "Authentication is required."),

            ForbidResult => ApiResponseFactory.Forbidden(
                context.HttpContext,
                "forbidden",
                "You do not have permission to perform this action."),

            NotFoundResult => ApiResponseFactory.NotFound(
                context.HttpContext,
                "not_found",
                "Resource was not found."),

            BadRequestResult => ApiResponseFactory.BadRequest(
                context.HttpContext,
                "validation_error",
                "The request is invalid."),

            ObjectResult objectResult => WrapObjectResult(context, objectResult),

            _ => result
        };
    }

    private static IActionResult WrapObjectResult(
        ResultExecutingContext context,
        ObjectResult objectResult)
    {
        var statusCode = ResolveStatusCode(objectResult);

        if (statusCode >= 200 && statusCode < 300)
        {
            if (IsSuccessEnvelope(objectResult.Value) || objectResult.Value is ApiErrorResponseDto)
                return objectResult;

            objectResult.Value = ApiResponseFactory.BuildSuccess(
                context.HttpContext,
                objectResult.Value,
                ResolveSuccessMessage(statusCode));

            return objectResult;
        }

        if (objectResult.Value is ApiErrorResponseDto)
            return objectResult;

        if (objectResult.Value is ValidationProblemDetails validationProblem)
        {
            var errors = validationProblem.Errors.ToDictionary(
                x => x.Key,
                x => x.Value);

            return new ObjectResult(ApiResponseFactory.Build(
                context.HttpContext,
                "validation_error",
                "One or more validation errors occurred.",
                errors))
            {
                StatusCode = statusCode
            };
        }

        if (objectResult.Value is ProblemDetails problemDetails)
        {
            return new ObjectResult(ApiResponseFactory.Build(
                context.HttpContext,
                ResolveErrorCode(statusCode),
                ResolveProblemMessage(problemDetails, statusCode)))
            {
                StatusCode = statusCode
            };
        }

        if (objectResult.Value is string message)
        {
            return new ObjectResult(ApiResponseFactory.Build(
                context.HttpContext,
                ResolveErrorCode(statusCode),
                message))
            {
                StatusCode = statusCode
            };
        }

        return objectResult;
    }

    private static int ResolveStatusCode(ObjectResult objectResult)
    {
        if (objectResult.StatusCode.HasValue)
            return objectResult.StatusCode.Value;

        return objectResult switch
        {
            CreatedAtActionResult => StatusCodes.Status201Created,
            CreatedAtRouteResult => StatusCodes.Status201Created,
            CreatedResult => StatusCodes.Status201Created,
            _ => StatusCodes.Status200OK
        };
    }

    private static string ResolveSuccessMessage(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status201Created => "Resource created successfully.",
            _ => "Request completed successfully."
        };
    }

    private static string ResolveErrorCode(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "validation_error",
            StatusCodes.Status401Unauthorized => "unauthorized",
            StatusCodes.Status403Forbidden => "forbidden",
            StatusCodes.Status404NotFound => "not_found",
            StatusCodes.Status409Conflict => "conflict",
            StatusCodes.Status500InternalServerError => "server_error",
            _ => "request_failed"
        };
    }

    private static string ResolveProblemMessage(ProblemDetails problemDetails, int statusCode)
    {
        if (!string.IsNullOrWhiteSpace(problemDetails.Detail))
            return problemDetails.Detail;

        if (!string.IsNullOrWhiteSpace(problemDetails.Title))
            return problemDetails.Title;

        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "The request is invalid.",
            StatusCodes.Status401Unauthorized => "Authentication is required.",
            StatusCodes.Status403Forbidden => "You do not have permission to perform this action.",
            StatusCodes.Status404NotFound => "Resource was not found.",
            StatusCodes.Status409Conflict => "The request conflicts with the current state.",
            _ => "Request failed."
        };
    }

    private static bool IsSuccessEnvelope(object? value)
    {
        if (value is null)
            return false;

        var valueType = value.GetType();

        return valueType.IsGenericType &&
               valueType.GetGenericTypeDefinition() == typeof(ApiSuccessResponseDto<>);
    }
}