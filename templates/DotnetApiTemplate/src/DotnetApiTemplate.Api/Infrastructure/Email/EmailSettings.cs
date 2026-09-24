namespace DotnetApiTemplate.Api.Infrastructure.Email;

/// <summary>SMTP settings from EMAIL_* environment variables (.env).</summary>
public sealed class EmailSettings
{
    /// <summary>SMTP host. Leave empty to log emails instead of sending them.</summary>
    [ConfigurationKeyName("EMAIL_HOST")]
    public string Host { get; init; } = string.Empty;

    [ConfigurationKeyName("EMAIL_PORT")]
    public int Port { get; init; } = 587;

    /// <summary>TLS: implicit on port 465, STARTTLS (required) on other ports. False for local servers like Mailpit.</summary>
    [ConfigurationKeyName("EMAIL_ENABLE_SSL")]
    public bool EnableSsl { get; init; } = true;

    [ConfigurationKeyName("EMAIL_USERNAME")]
    public string UserName { get; init; } = string.Empty;

    [ConfigurationKeyName("EMAIL_PASSWORD")]
    public string Password { get; init; } = string.Empty;

    [ConfigurationKeyName("EMAIL_FROM")]
    public string From { get; init; } = "no-reply@dotnetapitemplate.local";

    /// <summary>Sender display name, also available in templates as <c>{{ app_name }}</c>.</summary>
    [ConfigurationKeyName("EMAIL_FROM_NAME")]
    public string FromName { get; init; } = "DotnetApiTemplate";
}
