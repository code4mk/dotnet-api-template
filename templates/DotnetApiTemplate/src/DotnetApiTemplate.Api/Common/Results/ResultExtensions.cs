namespace DotnetApiTemplate.Api.Common.Results;

public static class ResultExtensions
{
    /// <summary>Converts a failed result to an RFC 7807 ProblemDetails response.</summary>
    public static ProblemHttpResult ToProblem(this Result result)
    {
        if (result.IsSuccess || result.Error is null)
        {
            throw new InvalidOperationException("Cannot convert a successful result to a problem.");
        }

        var error = result.Error;
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return TypedResults.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message);
    }
}
