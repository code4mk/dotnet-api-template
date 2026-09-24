using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DotnetApiTemplate.Api.Common.Errors;

/// <summary>How an exception is answered: status code, stable error code, safe message and log level.</summary>
internal sealed record ExceptionMapping(int StatusCode, string Code, string Detail, LogLevel LogLevel)
{
    /// <summary>Non-standard status (nginx convention) for requests the client aborted.</summary>
    public const int ClientClosedRequest = 499;

    /// <summary>
    /// Maps known exceptions to client-facing errors. Messages here are safe to return;
    /// exception messages are never returned outside Development.
    /// Add a case here when a library throws for something that is really a client error.
    /// </summary>
    public static ExceptionMapping From(Exception exception, HttpContext httpContext) => exception switch
    {
        OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
            new(ClientClosedRequest, "request_aborted", "The client closed the request.", LogLevel.Information),

        BadHttpRequestException { InnerException: JsonException json } bad =>
            new(bad.StatusCode, "invalid_json", InvalidJsonDetail(json), LogLevel.Warning),

        BadHttpRequestException bad =>
            new(bad.StatusCode, BadRequestCode(bad.StatusCode), BadRequestDetail(bad), LogLevel.Warning),

        JsonException json =>
            new(StatusCodes.Status400BadRequest, "invalid_json", InvalidJsonDetail(json), LogLevel.Warning),

        DbUpdateConcurrencyException =>
            new(StatusCodes.Status409Conflict, "concurrency_conflict",
                "The resource was changed by someone else. Reload it and try again.", LogLevel.Warning),

        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
            new(StatusCodes.Status409Conflict, "conflict", "A resource with the same unique value already exists.", LogLevel.Warning),

        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
            new(StatusCodes.Status409Conflict, "conflict", "The request refers to a resource that doesn't exist or is still in use.", LogLevel.Warning),

        NotImplementedException =>
            new(StatusCodes.Status501NotImplemented, "not_implemented", "This operation is not implemented yet.", LogLevel.Error),

        _ =>
            new(StatusCodes.Status500InternalServerError, "server_error",
                "An unexpected error occurred. Please try again later.", LogLevel.Error),
    };

    /// <summary>Where the JSON is wrong ($.price, line 3), without the serializer's internal message.</summary>
    private static string InvalidJsonDetail(JsonException exception)
    {
        var location = exception.Path is { Length: > 1 } path ? $" at '{path}'" : string.Empty;
        var line = exception.LineNumber is { } lineNumber ? $" (line {lineNumber + 1})" : string.Empty;
        return $"The request body is not valid JSON for this endpoint{location}{line}. "
            + "Check the value's type: numbers unquoted, enums by name, dates in ISO 8601, no duplicate properties.";
    }

    private static string BadRequestCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status413PayloadTooLarge => "payload_too_large",
        StatusCodes.Status415UnsupportedMediaType => "unsupported_media_type",
        StatusCodes.Status408RequestTimeout => "request_timeout",
        _ => "bad_request",
    };

    /// <summary>Client-friendly text for the framework's binding errors (its own messages are meant for developers).</summary>
    private static string BadRequestDetail(BadHttpRequestException exception) => exception switch
    {
        { StatusCode: StatusCodes.Status413PayloadTooLarge } => "The request body is too large.",
        { StatusCode: StatusCodes.Status415UnsupportedMediaType } => "Send the request body as JSON (Content-Type: application/json).",
        { StatusCode: StatusCodes.Status408RequestTimeout } => "The request body was not received in time.",
        _ when exception.Message.Contains("no body was provided", StringComparison.OrdinalIgnoreCase)
            => "A JSON request body is required.",
        _ when exception.Message.Contains("Required parameter", StringComparison.OrdinalIgnoreCase)
            => "A required parameter is missing.",
        _ when exception.Message.Contains("Failed to bind parameter", StringComparison.OrdinalIgnoreCase)
            => "A parameter has an invalid value.",
        _ => "The request is invalid.",
    };
}
