using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using DotnetApiTemplate.Api.Common.Cors;
using DotnetApiTemplate.Api.Common.Settings;
using DotnetApiTemplate.Api.Infrastructure.Authentication;
using DotnetApiTemplate.Api.Infrastructure.Email;

namespace DotnetApiTemplate.UnitTests.Common.Settings;

public sealed class SettingsDiscoveryTests
{
    private static readonly IConfiguration EmptyConfiguration = new ConfigurationBuilder().Build();

    [Fact]
    public void EnvSettingsTypes_FindsTheTemplateSettings()
    {
        Assert.Contains(typeof(AppSettings), SettingsExtensions.EnvSettingsTypes);
        Assert.Contains(typeof(DatabaseSettings), SettingsExtensions.EnvSettingsTypes);
        Assert.Contains(typeof(CorsSettings), SettingsExtensions.EnvSettingsTypes);
        Assert.Contains(typeof(JwtSettings), SettingsExtensions.EnvSettingsTypes);
        Assert.Contains(typeof(EmailSettings), SettingsExtensions.EnvSettingsTypes);
    }

    [Fact]
    public void AddAllEnvSettings_RegistersEverySettingsClassAndItsValidator()
    {
        var services = new ServiceCollection().AddAllEnvSettings(EmptyConfiguration);

        Assert.All(SettingsExtensions.EnvSettingsTypes, type =>
        {
            Assert.Contains(services, d => d.ServiceType == type);
            Assert.Contains(services, d => d.ServiceType == typeof(IValidateOptions<>).MakeGenericType(type));
        });
    }

    [Fact]
    public void AddEnvSettings_TwiceForTheSameClass_RegistersItOnce()
    {
        var services = new ServiceCollection()
            .AddAllEnvSettings(EmptyConfiguration)
            .AddEnvSettings<JwtSettings>(EmptyConfiguration);

        Assert.Single(services, d => d.ServiceType == typeof(JwtSettings));
    }

    [Fact]
    public void EveryClassWithEnvironmentVariableProperties_IsMarkedIEnvSettings()
    {
        // A settings class without the marker would never be registered (or validated).
        var unmarked = typeof(IEnvSettings).Assembly.GetTypes()
            .Where(type => type.IsClass
                && type.GetProperties().Any(p => p.GetCustomAttribute<ConfigurationKeyNameAttribute>() is not null)
                && !typeof(IEnvSettings).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .ToArray();

        Assert.True(unmarked.Length == 0, $"Add ': IEnvSettings' to: {string.Join(", ", unmarked)}");
    }

    [Fact]
    public void AddAllEnvSettings_BindsValuesFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DB_HOST"] = "db.internal",
                ["DB_PORT"] = "6543",
                ["DB_NAME"] = "app",
                ["DB_USER"] = "app",
                ["DB_PASSWORD"] = "secret",
            })
            .Build();

        using var provider = new ServiceCollection().AddAllEnvSettings(configuration).BuildServiceProvider();
        var settings = provider.GetRequiredService<DatabaseSettings>();

        Assert.Equal("db.internal", settings.Host);
        Assert.Equal(6543, settings.Port);
    }
}
