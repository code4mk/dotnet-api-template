using System.Reflection;
using DotnetApiTemplate.Api.Common.Settings;

namespace DotnetApiTemplate.Api.Features.Root;

public static class RootEndpoints
{
    private const string ApiName = "DotnetApiTemplate API";

    /// <summary>Version from the assembly (e.g. 1.0.0), without the "+commit" build metadata.</summary>
    private static readonly string Version =
        typeof(RootEndpoints).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0] ?? "unknown";

    public static IEndpointRouteBuilder MapRootEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", GetRoot)
            .WithTags("Root")
            .WithName("GetRoot")
            .WithSummary("Welcome message with the environment, version and useful links.")
            .AllowAnonymous();

        return app;
    }

    private static Ok<RootResponse> GetRoot(AppSettings settings, IHostEnvironment environment)
    {
        var links = new Dictionary<string, string>
        {
            ["health"] = "/health",
            ["products"] = "/api/products",
        };

        if (environment.IsDevelopment())
        {
            links["swagger"] = "/swagger";
            links["openapi"] = "/openapi/v1.json";
        }

        return TypedResults.Ok(new RootResponse(
            Name: ApiName,
            Message: $"Welcome to the {ApiName}. The service is up and running.",
            Environment: settings.Env,
            Version: Version,
            Links: links));
    }
}
