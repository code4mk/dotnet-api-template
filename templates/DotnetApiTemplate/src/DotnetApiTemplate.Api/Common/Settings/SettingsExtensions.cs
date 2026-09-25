using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DotnetApiTemplate.Api.Common.Settings;

public static class SettingsExtensions
{
    /// <summary>Every concrete <see cref="IEnvSettings"/> class in this project, ordered by name.</summary>
    public static IReadOnlyList<Type> EnvSettingsTypes { get; } = typeof(SettingsExtensions).Assembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEnvSettings).IsAssignableFrom(type))
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    private static readonly MethodInfo AddEnvSettingsMethod = typeof(SettingsExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(method => method is { Name: nameof(AddEnvSettings), IsGenericMethodDefinition: true });

    /// <summary>
    /// Registers every <see cref="IEnvSettings"/> class in the project (see <see cref="EnvSettingsTypes"/>),
    /// so a new settings class only needs <c>: IEnvSettings</c>. All of them are validated at startup.
    /// </summary>
    public static IServiceCollection AddAllEnvSettings(this IServiceCollection services, IConfiguration configuration)
    {
        foreach (var type in EnvSettingsTypes)
        {
            AddEnvSettingsMethod.MakeGenericMethod(type).Invoke(null, [services, configuration]);
        }

        return services;
    }

    /// <summary>
    /// Registers a typed settings class bound from environment variables, like a pydantic <c>BaseSettings</c>:
    /// <c>[ConfigurationKeyName("DB_HOST")]</c> names the variable, property initializers are the defaults and
    /// data annotations (<c>[Required]</c>, <c>[Range]</c>, ...) are checked when the app starts.
    /// Inject it directly (<c>DatabaseSettings settings</c>) or as <c>IOptions&lt;DatabaseSettings&gt;</c>.
    /// Classes marked <see cref="IEnvSettings"/> are registered automatically; calling this again is harmless.
    /// </summary>
    public static IServiceCollection AddEnvSettings<TSettings>(this IServiceCollection services, IConfiguration configuration)
        where TSettings : class
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(TSettings)))
        {
            return services;   // already registered
        }

        services.AddOptions<TSettings>()
            .Bind(configuration)
            .ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TSettings>, EnvSettingsValidator<TSettings>>());
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TSettings>>().Value);

        return services;
    }

    /// <summary>
    /// Validates every settings class now and reports all errors at once. Call right after
    /// <c>builder.Build()</c>, before anything (such as database setup) reads a settings class.
    /// </summary>
    public static void ValidateSettings(this WebApplication app) =>
        app.Services.GetRequiredService<IStartupValidator>().Validate();
}
