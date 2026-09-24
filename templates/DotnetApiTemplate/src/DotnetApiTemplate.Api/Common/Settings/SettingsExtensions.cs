using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DotnetApiTemplate.Api.Common.Settings;

public static class SettingsExtensions
{
    /// <summary>
    /// Registers a typed settings class bound from environment variables, like a pydantic <c>BaseSettings</c>:
    /// <c>[ConfigurationKeyName("DB_HOST")]</c> names the variable, property initializers are the defaults and
    /// data annotations (<c>[Required]</c>, <c>[Range]</c>, ...) are checked when the app starts.
    /// Inject it directly (<c>DatabaseSettings settings</c>) or as <c>IOptions&lt;DatabaseSettings&gt;</c>.
    /// </summary>
    public static IServiceCollection AddEnvSettings<TSettings>(this IServiceCollection services, IConfiguration configuration)
        where TSettings : class
    {
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
