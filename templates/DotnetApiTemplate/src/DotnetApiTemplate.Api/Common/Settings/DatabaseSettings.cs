using System.ComponentModel.DataAnnotations;
using Npgsql;

namespace DotnetApiTemplate.Api.Common.Settings;

/// <summary>PostgreSQL connection from DB_* environment variables (.env).</summary>
public sealed class DatabaseSettings : IEnvSettings
{
    [ConfigurationKeyName("DB_HOST")]
    [Required]
    public string Host { get; init; } = "localhost";

    [ConfigurationKeyName("DB_PORT")]
    [Range(1, 65535)]
    public int Port { get; init; } = 5432;

    [ConfigurationKeyName("DB_NAME")]
    [Required]
    public string Name { get; init; } = string.Empty;

    [ConfigurationKeyName("DB_USER")]
    [Required]
    public string User { get; init; } = string.Empty;

    [ConfigurationKeyName("DB_PASSWORD")]
    [Required]
    public string Password { get; init; } = string.Empty;

    public string ConnectionString => new NpgsqlConnectionStringBuilder
    {
        Host = Host,
        Port = Port,
        Database = Name,
        Username = User,
        Password = Password,
    }.ConnectionString;
}
