namespace DotnetApiTemplate.Api.Common.Cors;

/// <summary>
/// Browser origins allowed to call the API, from CORS_ALLOWED_ORIGINS (.env), comma-separated:
/// <c>https://app.example.com,http://localhost:5173</c>. Empty means no cross-origin browser access.
/// </summary>
public sealed class CorsSettings
{
    [ConfigurationKeyName("CORS_ALLOWED_ORIGINS")]
    [OriginList]
    public string AllowedOrigins { get; init; } = string.Empty;

    /// <summary>The parsed origins, without trailing slashes.</summary>
    public IReadOnlyList<string> Origins => Parse(AllowedOrigins);

    internal static string[] Parse(string? value) =>
        (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(origin => origin.TrimEnd('/'))
            .ToArray();
}
