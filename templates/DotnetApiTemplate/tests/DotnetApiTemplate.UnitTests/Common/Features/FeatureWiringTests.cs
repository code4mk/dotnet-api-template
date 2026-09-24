using Microsoft.Extensions.DependencyInjection;
using DotnetApiTemplate.Api.Common.Extensions;
using DotnetApiTemplate.Api.Common.Features;
using DotnetApiTemplate.Api.Features.Auth;
using DotnetApiTemplate.Api.Features.Products;
using DotnetApiTemplate.Api.Features.Users;

namespace DotnetApiTemplate.UnitTests.Common.Features;

/// <summary>
/// Guards feature wiring in both modes (manual and auto-discovery): a forgotten AddScoped line, or a
/// service whose name doesn't follow the XService : IXService convention, fails here, not in production.
/// </summary>
public sealed class FeatureWiringTests
{
    [Fact]
    public void AddFeatures_RegistersEveryFeatureServiceContract()
    {
        var services = new ServiceCollection().AddFeatures();

        var missing = FeatureDiscovery.ServiceContracts
            .Where(contract => !services.Any(d => d.ServiceType == contract))
            .Select(contract => contract.FullName)
            .ToArray();

        Assert.True(missing.Length == 0,
            $"Not registered in AddFeatures(): {string.Join(", ", missing)}. Add the AddScoped line (manual mode) " +
            "or name the implementation after the interface, IXService -> XService (auto-discovery).");
    }

    [Fact]
    public void Convention_FindsAnImplementationForEveryServiceContract()
    {
        // So switching to auto-discovery can't silently lose a service.
        var found = FeatureDiscovery.ServiceTypes.Select(pair => pair.Service).ToHashSet();

        var withoutConventionalImplementation = FeatureDiscovery.ServiceContracts.Where(c => !found.Contains(c)).ToArray();

        Assert.Empty(withoutConventionalImplementation);
    }

    [Fact]
    public void Convention_MatchesTheSampleFeatures()
    {
        Assert.Contains((typeof(IAuthService), typeof(AuthService)), FeatureDiscovery.ServiceTypes);
        Assert.Contains((typeof(IUserService), typeof(UserService)), FeatureDiscovery.ServiceTypes);
        Assert.Contains((typeof(IProductService), typeof(ProductService)), FeatureDiscovery.ServiceTypes);

        Assert.Contains(typeof(AuthEndpoints), FeatureDiscovery.EndpointTypes);
        Assert.Contains(typeof(UserEndpoints), FeatureDiscovery.EndpointTypes);
        Assert.Contains(typeof(ProductEndpoints), FeatureDiscovery.EndpointTypes);
    }

    [Fact]
    public void AddFeatures_RegistersFeatureServicesAsScoped()
    {
        var services = new ServiceCollection().AddFeatures();

        Assert.All(FeatureDiscovery.ServiceContracts, contract =>
            Assert.Equal(ServiceLifetime.Scoped, services.Single(d => d.ServiceType == contract).Lifetime));
    }
}
