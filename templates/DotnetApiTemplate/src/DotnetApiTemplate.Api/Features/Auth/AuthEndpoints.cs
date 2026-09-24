using DotnetApiTemplate.Api.Common.Features;
using DotnetApiTemplate.Api.Common.Results;

namespace DotnetApiTemplate.Api.Features.Auth;

public sealed class AuthEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/auth")
            .WithTags("Auth")
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Get an access token.")
            .ProducesValidationProblem();
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Login(
        LoginRequest request,
        IAuthService service,
        CancellationToken cancellationToken)
    {
        var result = await service.LoginAsync(request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }
}
