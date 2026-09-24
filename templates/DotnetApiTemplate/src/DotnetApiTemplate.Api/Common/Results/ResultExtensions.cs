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

        return TypedResults.Problem(
            statusCode: error.Type.ToStatusCode(),
            title: error.Code,
            detail: error.Message);
    }
}
