namespace DotnetApiTemplate.Api.Common.Errors.Exceptions;

/// <summary>
/// Base class for exceptions the API answers deliberately: <see cref="GlobalExceptionHandler"/> turns them
/// into ProblemDetails with <see cref="StatusCode"/>, <see cref="Code"/> as the title and the message as
/// the detail. The message is sent to clients, so keep internals (SQL, stack details) out of it.
/// </summary>
/// <remarks>
/// Prefer returning a <c>Result</c> from services for expected failures; throw these where returning one
/// is impractical (deep helpers, domain entities) or from a feature's error catalog:
/// <c>throw UserErrors.NotFound(id).ToException();</c>
/// </remarks>
public abstract class AppException : Exception
{
    protected AppException(int statusCode, string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Code = code;
    }

    /// <summary>HTTP status of the response.</summary>
    public int StatusCode { get; }

    /// <summary>Stable, machine-readable error code, e.g. <c>users.not_found</c>.</summary>
    public string Code { get; }
}
