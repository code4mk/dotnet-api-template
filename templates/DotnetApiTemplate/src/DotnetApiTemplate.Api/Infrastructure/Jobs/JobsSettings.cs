using System.ComponentModel.DataAnnotations;
using DotnetApiTemplate.Api.Common.Settings;

namespace DotnetApiTemplate.Api.Infrastructure.Jobs;

/// <summary>Background job (Hangfire) settings from JOBS_* environment variables (.env).</summary>
public sealed class JobsSettings : IEnvSettings
{
    /// <summary>
    /// <c>true</c> (default): Hangfire with PostgreSQL: jobs are stored, run by a worker and retried; schedules run.
    /// <c>false</c>: no Hangfire at all: enqueued jobs run immediately in the request (no retries), delayed
    /// jobs are rejected, recurring jobs don't run. Features use the same code in both modes.
    /// </summary>
    [ConfigurationKeyName("JOBS_ENABLED")]
    public bool Enabled { get; init; } = true;

    /// <summary>Reads JOBS_ENABLED before the settings are validated (for registration decisions).</summary>
    public static bool EnabledFor(IConfiguration configuration) =>
        !bool.TryParse(configuration["JOBS_ENABLED"], out var enabled) || enabled;

    /// <summary>Jobs processed in parallel by one worker process.</summary>
    [ConfigurationKeyName("JOBS_WORKER_COUNT")]
    [Range(1, 100)]
    public int WorkerCount { get; init; } = 10;

    /// <summary>Queues this process handles, highest priority first (comma-separated).</summary>
    [ConfigurationKeyName("JOBS_QUEUES")]
    [Required]
    public string QueueList { get; init; } = $"{JobQueues.Default},{JobQueues.Emails}";

    /// <summary>How often idle workers check for new jobs.</summary>
    [ConfigurationKeyName("JOBS_POLL_INTERVAL_SECONDS")]
    [Range(1, 300)]
    public int PollIntervalSeconds { get; init; } = 5;

    /// <summary>
    /// How long a stopping worker waits for running jobs; jobs still running are re-queued. Keep it below
    /// the process manager's stop timeout (supervisor stopwaitsecs, docker stop_grace_period).
    /// </summary>
    [ConfigurationKeyName("JOBS_SHUTDOWN_TIMEOUT_SECONDS")]
    [Range(1, 3600)]
    public int ShutdownTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Dashboard (/jobs) login. In Development the dashboard is open; elsewhere it's served only when both
    /// values are set, behind HTTP basic auth (use HTTPS).
    /// </summary>
    [ConfigurationKeyName("JOBS_DASHBOARD_USERNAME")]
    public string DashboardUsername { get; init; } = string.Empty;

    [ConfigurationKeyName("JOBS_DASHBOARD_PASSWORD")]
    public string DashboardPassword { get; init; } = string.Empty;

    public string[] Queues => QueueList
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(queue => queue.ToLowerInvariant())
        .ToArray();

    public bool HasDashboardCredentials =>
        !string.IsNullOrWhiteSpace(DashboardUsername) && !string.IsNullOrWhiteSpace(DashboardPassword);
}

/// <summary>Queue names (lowercase letters, digits, underscores). Use with <c>[Queue(JobQueues.Emails)]</c>.</summary>
public static class JobQueues
{
    public const string Default = "default";
    public const string Emails = "emails";
}
