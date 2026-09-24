using System.ComponentModel.DataAnnotations;

namespace DotnetApiTemplate.Api.Common.Settings;

/// <summary>Project-wide settings from the environment (.env).</summary>
public sealed class AppSettings
{
    /// <summary>dev, stage or prod. Also sets the ASP.NET Core environment (see <see cref="EnvFile"/>).</summary>
    [ConfigurationKeyName("APP_ENV")]
    [Required, AllowedValues("dev", "stage", "prod")]
    public string Env { get; init; } = "dev";

    public bool IsDev => Env == "dev";

    public bool IsStage => Env == "stage";

    public bool IsProd => Env == "prod";
}
