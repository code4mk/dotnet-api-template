using System.ComponentModel.DataAnnotations;

namespace DotnetApiTemplate.Api.Common.Settings;

/// <summary>Project-wide settings from the environment (.env).</summary>
public sealed class AppSettings : IEnvSettings
{
    /// <summary>dev, stage or prod. Also sets the ASP.NET Core environment (see <see cref="EnvFile"/>).</summary>
    [ConfigurationKeyName("APP_ENV")]
    [Required, AllowedValues("dev", "stage", "prod")]
    public string Env { get; init; } = "dev";

    /// <summary>
    /// What this process runs: <c>all</c> (API + background job server, the default), <c>api</c> (HTTP only;
    /// enqueues jobs) or <c>worker</c> (background jobs and recurring schedules; only /health over HTTP).
    /// </summary>
    [ConfigurationKeyName("APP_ROLE")]
    [Required, AllowedValues("all", "api", "worker")]
    public string Role { get; init; } = "all";

    /// <summary>Serves the HTTP API (roles <c>all</c> and <c>api</c>).</summary>
    public bool RunsApi => Role is "all" or "api";

    /// <summary>Runs the background job server (roles <c>all</c> and <c>worker</c>).</summary>
    public bool RunsJobs => Role is "all" or "worker";

    /// <summary>Reads APP_ROLE before the settings are validated (for registration decisions).</summary>
    public static bool RunsJobsFor(IConfiguration configuration) =>
        (configuration["APP_ROLE"] ?? "all").Trim().ToLowerInvariant() is "all" or "worker";

    public bool IsDev => Env == "dev";

    public bool IsStage => Env == "stage";

    public bool IsProd => Env == "prod";
}
