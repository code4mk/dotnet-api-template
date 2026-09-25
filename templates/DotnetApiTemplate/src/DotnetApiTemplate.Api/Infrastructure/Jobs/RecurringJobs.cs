using Hangfire;
using DotnetApiTemplate.Api.Features.Products.Jobs;

namespace DotnetApiTemplate.Api.Infrastructure.Jobs;

/// <summary>
/// Every recurring (cron) job, in one place. Registered when a job server starts (APP_ROLE all/worker);
/// with several workers each one registers them, and Hangfire still runs every schedule once.
/// Removing a line here doesn't delete the schedule from the database: call RemoveIfExists("id") for that.
/// </summary>
public static class RecurringJobs
{
    public static void Register(IRecurringJobManager jobs)
    {
        var utc = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };

        // Cron: minute hour day-of-month month day-of-week. Cron.Daily(6) = "0 6 * * *" (06:00 UTC).
        jobs.AddOrUpdate<LowStockReportJob>(
            "products.low-stock-report",
            JobQueues.Default,
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(6),
            utc);
    }
}
