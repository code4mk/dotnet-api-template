using System.ComponentModel.DataAnnotations;

namespace DotnetApiTemplate.Api.Infrastructure.Authentication;

/// <summary>JWT settings from JWT_* environment variables (.env).</summary>
public sealed class JwtSettings
{
    [ConfigurationKeyName("JWT_ISSUER")]
    [Required]
    public string Issuer { get; init; } = "DotnetApiTemplate";

    [ConfigurationKeyName("JWT_AUDIENCE")]
    [Required]
    public string Audience { get; init; } = "DotnetApiTemplate";

    /// <summary>HMAC key. Use at least 32 characters. Keep real keys out of committed files.</summary>
    [ConfigurationKeyName("JWT_SIGNING_KEY")]
    [Required, MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [ConfigurationKeyName("JWT_EXPIRY_MINUTES")]
    [Range(1, 1440)]
    public int ExpiryMinutes { get; init; } = 60;
}
