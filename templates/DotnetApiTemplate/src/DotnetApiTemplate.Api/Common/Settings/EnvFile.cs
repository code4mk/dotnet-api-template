using DotNetEnv;

namespace DotnetApiTemplate.Api.Common.Settings;

/// <summary>
/// Loads the <c>.env</c> file into environment variables and sets the ASP.NET Core environment from APP_ENV.
/// Variables that are already set (Docker, CI, your shell) always win over the file.
/// </summary>
/// <remarks>
/// After loading, read raw values anywhere with DotNetEnv's typed getters, e.g.
/// <c>Env.GetString("DB_HOST")</c>, <c>Env.GetInt("DB_PORT", 5432)</c>, <c>Env.GetBool("FEATURE_X", false)</c>.
/// Prefer the typed settings classes (see <see cref="SettingsExtensions"/>) for anything the app depends on.
/// </remarks>
public static class EnvFile
{
    private static readonly Dictionary<string, string> AppEnvironments = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dev"] = "Development",
        ["stage"] = "Staging",
        ["prod"] = "Production",
    };

    /// <summary>Call before <c>WebApplication.CreateBuilder</c>.</summary>
    public static void Load()
    {
        // TraversePath: find .env in the current directory or a parent (the repository root).
        // NoClobber: never overwrite variables that are already set.
        Env.TraversePath().NoClobber().Load();

        var appEnv = Env.GetString("APP_ENV");
        if (string.IsNullOrWhiteSpace(appEnv))
        {
            return;
        }

        if (!AppEnvironments.TryGetValue(appEnv, out var environmentName))
        {
            throw new InvalidOperationException(
                $"APP_ENV must be one of: {string.Join(", ", AppEnvironments.Keys)} (was '{appEnv}').");
        }

        // APP_ENV is the source of truth for the ASP.NET Core environment.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentName);
    }
}
