namespace DotnetApiTemplate.Api.Common.Errors.Exceptions;

// The most used API exceptions. Each maps to one HTTP status; the message is shown to clients.

/// <summary>400: the input is invalid. Optionally with field errors, like built-in validation.</summary>
public sealed class RequestValidationException : AppException
{
    public RequestValidationException(string code, string message, IDictionary<string, string[]>? errors = null)
        : base(StatusCodes.Status400BadRequest, code, message) =>
        Errors = errors is null ? new Dictionary<string, string[]>() : new Dictionary<string, string[]>(errors);

    /// <summary>One field error: <c>throw RequestValidationException.ForField("startDate", "Must be before endDate.");</c></summary>
    public static RequestValidationException ForField(string field, string error) =>
        new("validation_failed", "One or more fields are invalid.", new Dictionary<string, string[]> { [field] = [error] });

    /// <summary>Field name → messages; sent as <c>errors</c>, the same shape as built-in validation.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

/// <summary>401: not authenticated, or the credentials are invalid.</summary>
public sealed class UnauthorizedException(string code = "unauthorized", string message = "Authentication is required.")
    : AppException(StatusCodes.Status401Unauthorized, code, message);

/// <summary>403: authenticated, but not allowed to do this.</summary>
public sealed class ForbiddenException(string code = "forbidden", string message = "You are not allowed to perform this action.")
    : AppException(StatusCodes.Status403Forbidden, code, message);

/// <summary>404: the resource doesn't exist (or the caller may not know it exists).</summary>
public sealed class NotFoundException(string code, string message)
    : AppException(StatusCodes.Status404NotFound, code, message)
{
    /// <summary><c>throw NotFoundException.For("Product", 42);</c> → <c>product.not_found</c>, "Product with id 42 was not found."</summary>
    public static NotFoundException For(string resource, object id) =>
        new($"{resource.ToLowerInvariant()}.not_found", $"{resource} with id {id} was not found.");
}

/// <summary>409: conflicts with the current state, e.g. a duplicate or a stale update.</summary>
public sealed class ConflictException(string code, string message)
    : AppException(StatusCodes.Status409Conflict, code, message);

/// <summary>422: the request is valid, but a business rule doesn't allow it (e.g. deleting a product with open orders).</summary>
public sealed class BusinessRuleException(string code, string message)
    : AppException(StatusCodes.Status422UnprocessableEntity, code, message);

/// <summary>
/// 502 (or 503): an external service (payment provider, other API) failed. The message is shown to clients;
/// pass the original exception as <paramref name="innerException"/> so it's logged with its stack trace.
/// </summary>
public sealed class ExternalServiceException(
    string code,
    string message,
    Exception? innerException = null,
    bool unavailable = false)
    : AppException(unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status502BadGateway, code, message, innerException);
