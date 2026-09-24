using System.Reflection;

namespace DotnetApiTemplate.Api.Common.Features;

/// <summary>
/// Finds feature endpoints and services in this assembly, in <c>Features/</c> and any nested folder.
/// Whether they're wired automatically is decided by <see cref="AutoDiscovery"/>.
/// </summary>
public static class FeatureDiscovery
{
    /// <summary>
    /// <c>false</c> (default): register services and map endpoints by hand in <c>AddFeatures()</c> and
    /// <c>MapFeatures()</c>. <c>true</c>: every <see cref="IEndpoints"/> class is mapped and every
    /// <c>XService : IXService</c> is registered (scoped) automatically.
    /// It's a code switch on purpose, not a setting: which routes exist must not depend on an environment variable.
    /// Tests check both modes the same way: every feature service is registered and every IEndpoints class is mapped.
    /// </summary>
    public static readonly bool AutoDiscovery = false;

    private const string FeaturesNamespace = "DotnetApiTemplate.Api.Features";

    private static readonly Type[] FeatureTypes = typeof(FeatureDiscovery).Assembly
        .GetTypes()
        .Where(type => type.Namespace?.StartsWith(FeaturesNamespace, StringComparison.Ordinal) == true)
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    /// <summary>Every concrete <see cref="IEndpoints"/> class, ordered by name.</summary>
    public static IReadOnlyList<Type> EndpointTypes { get; } = FeatureTypes
        .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEndpoints).IsAssignableFrom(type))
        .ToArray();

    /// <summary>
    /// Feature services by convention: a concrete class <c>XService</c> implementing an interface named
    /// <c>IXService</c>. Anything else (other lifetimes, several interfaces) is registered by hand.
    /// </summary>
    public static IReadOnlyList<(Type Service, Type Implementation)> ServiceTypes { get; } = FeatureTypes
        .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } && !IsCompilerGenerated(type))
        .SelectMany(type => type.GetInterfaces()
            .Where(contract => contract.Name == $"I{type.Name}")
            .Select(contract => (contract, type)))
        .ToArray();

    /// <summary>Every interface in <c>Features/</c> named <c>I...Service</c>: each needs a registered implementation.</summary>
    public static IReadOnlyList<Type> ServiceContracts { get; } = FeatureTypes
        .Where(type => type.IsInterface && type.Name.StartsWith('I') && type.Name.EndsWith("Service", StringComparison.Ordinal))
        .ToArray();

    private static bool IsCompilerGenerated(Type type) =>
        type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false);
}
