using Hangfire;
using Npgsql;
using DotnetApiTemplate.Api.Common.Settings;

namespace DotnetApiTemplate.Api.Infrastructure.Jobs;

/// <summary>
/// Runs when a job server starts (APP_ROLE all/worker), before Hangfire's own server:
/// stops startup with a clear message if Hangfire's tables are missing, then registers the recurring jobs.
/// </summary>
internal sealed class JobsStartupService(
    DatabaseSettings database,
    IRecurringJobManager recurringJobs,
    ILogger<JobsStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureSchemaExistsAsync(cancellationToken);

        RecurringJobs.Register(recurringJobs);
        logger.LogInformation("Background job server starting; recurring jobs registered");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureSchemaExistsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand($"SELECT to_regclass('\"{HangfireSchema.SchemaName}\".\"job\"') IS NOT NULL", connection);
        var exists = (bool)(await command.ExecuteScalarAsync(cancellationToken))!;

        if (!exists)
        {
            throw new InvalidOperationException(
                $"Hangfire's tables (schema \"{HangfireSchema.SchemaName}\") don't exist. " +
                "Apply the migrations first: dotnet ef database update --project src/DotnetApiTemplate.Api");
        }
    }
}
