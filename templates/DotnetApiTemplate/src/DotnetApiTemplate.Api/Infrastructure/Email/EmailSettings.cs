namespace DotnetApiTemplate.Api.Infrastructure.Email;

/// <summary>SMTP settings from EMAIL_* environment variables (.env).</summary>
public sealed class EmailSettings
{
    /// <summary>SMTP host. Leave empty to log emails instead of sending them (local development).</summary>
    [ConfigurationKeyName("EMAIL_HOST")]
    public string Host { get; init; } = string.Empty;

    [ConfigurationKeyName("EMAIL_PORT")]
    public int Port { get; init; } = 587;

    [ConfigurationKeyName("EMAIL_ENABLE_SSL")]
    public bool EnableSsl { get; init; } = true;

    [ConfigurationKeyName("EMAIL_USERNAME")]
    public string UserName { get; init; } = string.Empty;

    [ConfigurationKeyName("EMAIL_PASSWORD")]
    public string Password { get; init; } = string.Empty;

    [ConfigurationKeyName("EMAIL_FROM")]
    public string From { get; init; } = "no-reply@dotnetapitemplate.local";
}
