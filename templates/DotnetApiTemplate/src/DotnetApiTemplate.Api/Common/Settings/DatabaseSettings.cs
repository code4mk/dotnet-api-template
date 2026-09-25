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

    /// <summary>
    /// Connections per process (Npgsql's default is 100). With many instances, keep
    /// instances × processes × DB_MAX_POOL_SIZE below the server's max_connections, or use a pooler (PgBouncer, RDS Proxy).
    /// </summary>
    [ConfigurationKeyName("DB_MAX_POOL_SIZE")]
    [Range(1, 1000)]
    public int MaxPoolSize { get; init; } = 100;

    public string ConnectionString => new NpgsqlConnectionStringBuilder
    {
        Host = Host,
        Port = Port,
        Database = Name,
        Username = User,
        Password = Password,
        MaxPoolSize = MaxPoolSize,
    }.ConnectionString;
}
