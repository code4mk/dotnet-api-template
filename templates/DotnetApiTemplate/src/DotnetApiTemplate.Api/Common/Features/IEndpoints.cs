namespace DotnetApiTemplate.Api.Common.Features;

/// <summary>
/// A feature's endpoints. Implement it in <c>XEndpoints</c> and map routes on <paramref name="api"/>, the
/// <c>/api</c> group: every route requires authentication unless it calls <c>AllowAnonymous()</c>.
/// Mapped by <c>MapFeatures()</c>, manually or by auto-discovery (see <see cref="FeatureDiscovery"/>).
/// </summary>
public interface IEndpoints
{
    void MapEndpoints(IEndpointRouteBuilder api);
}

/// <summary>Endpoint metadata naming the <see cref="IEndpoints"/> class that mapped a route (used by tests and logs).</summary>
public sealed record FeatureEndpointsMetadata(Type EndpointsType);
