using DotnetApiTemplate.Api.Common.Features;
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
    public static WebApplication MapFeatures(this WebApplication app)
    {
        EnsureFeatureServicesRegistered(app);

        app.MapRootEndpoints();

        var api = app.MapGroup("/api")
            .RequireAuthorization();

        if (FeatureDiscovery.AutoDiscovery)
        {
            foreach (var type in FeatureDiscovery.EndpointTypes)
            {
                api.MapEndpoints(type);
            }
        }
        else
        {
            // Manual mode (default): one line per feature.
            api.MapEndpoints<AuthEndpoints>();
            api.MapEndpoints<UserEndpoints>();
            api.MapEndpoints<ProductEndpoints>();
        }

        app.Logger.LogInformation("Mapped feature endpoints ({Mode}): {Endpoints}",
            FeatureDiscovery.AutoDiscovery ? "auto-discovery" : "manual",
            string.Join(", ", MappedTypes(app).Select(type => type.Name)));

        return app;
    }

    public static IEndpointRouteBuilder MapEndpoints<TEndpoints>(this IEndpointRouteBuilder api)
        where TEndpoints : IEndpoints, new() =>
        api.MapEndpoints(typeof(TEndpoints));

    /// <summary>Maps one <see cref="IEndpoints"/> class; its routes carry <see cref="FeatureEndpointsMetadata"/>.</summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder api, Type endpointsType)
    {
        var endpoints = (IEndpoints)Activator.CreateInstance(endpointsType)!;
        var group = api.MapGroup(string.Empty).WithMetadata(new FeatureEndpointsMetadata(endpointsType));
        endpoints.MapEndpoints(group);
        return api;
    }

    /// <summary>
    /// Stops startup with a clear message when a feature service isn't registered, instead of the framework's
    /// "Body was inferred..." error when the endpoint is built.
    /// </summary>
    private static void EnsureFeatureServicesRegistered(WebApplication app)
    {
        var isService = app.Services.GetRequiredService<IServiceProviderIsService>();
        var missing = FeatureDiscovery.ServiceContracts.Where(contract => !isService.IsService(contract)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Feature services not registered: {string.Join(", ", missing.Select(t => t.Name))}. " +
                (FeatureDiscovery.AutoDiscovery
                    ? "Auto-discovery registers XService : IXService only: name the implementation after the interface, or register it in AddFeatures()."
                    : "Add services.AddScoped<IXService, XService>() to AddFeatures(), or enable FeatureDiscovery.AutoDiscovery."));
        }
    }

    /// <summary>The <see cref="IEndpoints"/> classes that mapped at least one route.</summary>
    public static IReadOnlyList<Type> MappedTypes(IEndpointRouteBuilder app) => app.DataSources
        .SelectMany(source => source.Endpoints)
        .Select(endpoint => endpoint.Metadata.GetMetadata<FeatureEndpointsMetadata>()?.EndpointsType)
        .OfType<Type>()
        .Distinct()
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .ToArray();
}
