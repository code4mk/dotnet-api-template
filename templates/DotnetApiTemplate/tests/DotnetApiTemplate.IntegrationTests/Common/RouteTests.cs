using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using DotnetApiTemplate.Api.Common.Features;

namespace DotnetApiTemplate.IntegrationTests.Common;

/// <summary>
/// Guards the route table in both wiring modes: every IEndpoints class is mapped, and the list of routes
/// with their auth requirement matches routes.snapshot.txt. A new, removed or newly public route fails
/// this test, so it always shows up in code review.
/// Update the snapshot after an intended change: UPDATE_SNAPSHOTS=1 dotnet test
/// </summary>
public sealed class RouteTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void EveryEndpointsClass_IsMapped()
    {
        var mapped = Endpoints()
            .Select(e => e.Metadata.GetMetadata<FeatureEndpointsMetadata>()?.EndpointsType)
            .OfType<Type>()
            .ToHashSet();

        var notMapped = FeatureDiscovery.EndpointTypes.Where(type => !mapped.Contains(type)).Select(t => t.FullName).ToArray();

        Assert.True(notMapped.Length == 0,
            $"Not mapped in MapFeatures(): {string.Join(", ", notMapped)}. Add api.MapEndpoints<T>() (manual mode).");
    }

    [Fact]
    public void Routes_MatchSnapshot()
    {
        var actual = string.Join('\n', Endpoints().Select(Describe).Distinct().Order(StringComparer.Ordinal)) + "\n";
        var snapshotPath = SnapshotPath();

        if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1")
        {
            File.WriteAllText(snapshotPath, actual);
            return;
        }

        var expected = File.Exists(snapshotPath) ? File.ReadAllText(snapshotPath).ReplaceLineEndings("\n") : string.Empty;

        Assert.True(expected == actual,
            $"The routes changed. If that's intended, run `UPDATE_SNAPSHOTS=1 dotnet test` and commit {Path.GetFileName(snapshotPath)}.\n" +
            $"Expected:\n{expected}\nActual:\n{actual}");
    }

    private RouteEndpoint[] Endpoints()
    {
        using var client = factory.CreateClient();   // starts the app so all endpoints are built
        return factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToArray();
    }

    /// <summary>"GET /api/users/{id:int} | auth | UserEndpoints"</summary>
    private static string Describe(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods is { Count: > 0 } list
            ? string.Join(",", list)
            : "ANY";

        var metadata = endpoint.Metadata;
        var policies = metadata.GetOrderedMetadata<IAuthorizeData>().Select(a => a.Policy).OfType<string>().Distinct().ToArray();
        var access = metadata.GetMetadata<IAllowAnonymous>() is not null ? "public"
            : policies.Length > 0 ? $"policy:{string.Join("+", policies)}"
            : metadata.GetMetadata<IAuthorizeData>() is not null ? "auth"
            : "public";

        var feature = metadata.GetMetadata<FeatureEndpointsMetadata>()?.EndpointsType.Name ?? "-";

        return $"{methods} {endpoint.RoutePattern.RawText} | {access} | {feature}";
    }

    private static string SnapshotPath([CallerFilePath] string testFile = "") =>
        Path.Combine(Path.GetDirectoryName(testFile)!, "routes.snapshot.txt");
}
