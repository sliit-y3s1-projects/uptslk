using api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace api.Common.Middleware;

// Turns any exception thrown during a request into a ProblemDetails JSON response.
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const string UnexpectedErrorMessage = "An unexpected error occurred.";

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AppException)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        // Unexpected errors can contain internal details, so the caller only gets a generic message.
        var statusCode = exception is AppException appException ? appException.StatusCode : StatusCodes.Status500InternalServerError;
        var message = exception is AppException ? exception.Message : UnexpectedErrorMessage;

        var problem = new ProblemDetails { Status = statusCode, Detail = message };
        // The web and mobile apps read the "error" field first.
        problem.Extensions["error"] = message;
        if (exception is BadRequestException { Errors.Count: > 0 } badRequest)
            problem.Extensions["errors"] = badRequest.Errors;
        if (exception is AppException { Details.Count: > 0 } withDetails)
            foreach (var (key, value) in withDetails.Details) problem.Extensions[key] = value;

        context.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
