using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DotnetApiTemplate.Api.Common.Errors.Exceptions;

namespace DotnetApiTemplate.Api.Common.Errors;

/// <summary>
/// Turns every unhandled exception into an RFC 7807 ProblemDetails response.
/// Client errors (bad JSON, missing body, unique violations, ...) get a 4xx and a one-line warning;
/// real failures get a 500 and are logged with the stack trace (see <see cref="ExceptionMapping"/>).
/// Expected business failures should still be returned as <c>Result</c> errors, not thrown.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var mapping = ExceptionMapping.From(exception, httpContext);
        Log(httpContext, exception, mapping);

        if (httpContext.Response.HasStarted)
        {
            // Headers are already sent; nothing more can be written. Let the server abort the response.
            return false;
        }

        httpContext.Response.StatusCode = mapping.StatusCode;
        if (mapping.StatusCode == ExceptionMapping.ClientClosedRequest)
        {
            return true;   // nobody is listening for a body
        }

        // Field errors use the same "errors" shape as built-in validation.
        var problem = exception is RequestValidationException { Errors.Count: > 0 } validation
            ? new HttpValidationProblemDetails(validation.Errors.ToDictionary())
            : new ProblemDetails();
        problem.Status = mapping.StatusCode;
        problem.Title = mapping.Code;
        problem.Detail = mapping.Detail;

        if (environment.IsDevelopment())
        {
            problem.Extensions["exception"] = new
            {
                type = exception.GetType().FullName,
                message = exception.Message,
                inner = exception.InnerException?.Message,
            };
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }

    private void Log(HttpContext httpContext, Exception exception, ExceptionMapping mapping)
    {
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path;

        if (mapping.LogLevel >= LogLevel.Error)
        {
            // Unexpected: keep the full stack trace.
            logger.Log(mapping.LogLevel, exception, "Unhandled exception for {Method} {Path} -> {StatusCode} {ErrorCode}",
                method, path, mapping.StatusCode, mapping.Code);
        }
        else
        {
            // Client-caused: one line, no stack trace.
            logger.Log(mapping.LogLevel, "Request {Method} {Path} failed -> {StatusCode} {ErrorCode}: {ExceptionType}: {ExceptionMessage}",
                method, path, mapping.StatusCode, mapping.Code, exception.GetType().Name, exception.Message);
        }
    }
}
