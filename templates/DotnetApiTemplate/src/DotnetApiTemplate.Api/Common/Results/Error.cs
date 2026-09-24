using DotnetApiTemplate.Api.Common.Errors.Exceptions;

namespace DotnetApiTemplate.Api.Common.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,

    /// <summary>The request is valid, but a business rule doesn't allow it (422).</summary>
    BusinessRule
}

/// <summary>
/// A business error. <see cref="Code"/> is stable and safe to show to clients.
/// Define them per feature (<c>UserErrors</c>, <c>ProductErrors</c>), then either return one
/// (<c>return UserErrors.NotFound(id);</c>) or throw it (<c>throw UserErrors.NotFound(id).ToException();</c>):
/// both produce the same ProblemDetails response.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public static Error BusinessRule(string code, string message) => new(code, message, ErrorType.BusinessRule);

    /// <summary>The matching exception, for code that throws instead of returning a <see cref="Result"/>.</summary>
    public AppException ToException() => Type switch
    {
        ErrorType.Validation => new RequestValidationException(Code, Message),
        ErrorType.NotFound => new NotFoundException(Code, Message),
        ErrorType.Conflict => new ConflictException(Code, Message),
        ErrorType.Unauthorized => new UnauthorizedException(Code, Message),
        ErrorType.Forbidden => new ForbiddenException(Code, Message),
        ErrorType.BusinessRule => new BusinessRuleException(Code, Message),
        _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unknown error type."),
    };
}

public static class ErrorTypeExtensions
{
    /// <summary>The one mapping from error type to HTTP status, used by Result and exceptions alike.</summary>
    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError,
    };
}
