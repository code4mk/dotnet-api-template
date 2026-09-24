namespace DotnetApiTemplate.Api.Common.Settings;

/// <summary>Development seed data from SEED_* environment variables (.env). Empty values skip seeding.</summary>
public sealed class SeedSettings
{
    [ConfigurationKeyName("SEED_ADMIN_EMAIL")]
    public string AdminEmail { get; init; } = string.Empty;

    [ConfigurationKeyName("SEED_ADMIN_PASSWORD")]
    public string AdminPassword { get; init; } = string.Empty;
}
