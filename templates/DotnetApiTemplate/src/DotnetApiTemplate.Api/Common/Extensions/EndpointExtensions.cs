using DotnetApiTemplate.Api.Features.Auth;
using DotnetApiTemplate.Api.Features.Products;
using DotnetApiTemplate.Api.Features.Root;
using DotnetApiTemplate.Api.Features.Users;

namespace DotnetApiTemplate.Api.Common.Extensions;

public static class EndpointExtensions
{
    /// <summary>
    /// Maps the public root endpoint (GET /) and every feature under /api. All /api endpoints require
    /// authentication by default; endpoints that must be public opt out with <c>AllowAnonymous()</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapFeatures(this IEndpointRouteBuilder app)
    {
        app.MapRootEndpoints();

        var api = app.MapGroup("/api")
            .RequireAuthorization();

        api.MapAuthEndpoints();
        api.MapUserEndpoints();
        api.MapProductEndpoints();

        return app;
    }
}
