using DotnetApiTemplate.Api.Features.Auth;
using DotnetApiTemplate.Api.Features.Products;
using DotnetApiTemplate.Api.Features.Users;

namespace DotnetApiTemplate.Api.Common.Extensions;

public static class EndpointExtensions
{
    /// <summary>
    /// Maps every feature under /api. All endpoints require authentication by default;
    /// endpoints that must be public opt out with <c>AllowAnonymous()</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapFeatures(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api")
            .RequireAuthorization();

        api.MapAuthEndpoints();
        api.MapUserEndpoints();
        api.MapProductEndpoints();

        return app;
    }
}
