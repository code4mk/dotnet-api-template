using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using DotnetApiTemplate.Api.Common.Settings;

namespace DotnetApiTemplate.Api.Infrastructure.Jobs;

public static class JobsExtensions
{
    public const string DashboardPath = "/jobs";

    /// <summary>
    /// Hangfire with PostgreSQL storage (schema "hangfire", created by EF migrations, never at startup).
    /// Every role can enqueue jobs (<c>IBackgroundJobClient</c>); roles <c>all</c> and <c>worker</c> also run the
    /// job server, which executes jobs and triggers delayed and recurring ones.
    /// </summary>
    public static IServiceCollection AddJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire((sp, config) =>
        {
            var database = sp.GetRequiredService<DatabaseSettings>();
            var jobs = sp.GetRequiredService<JobsSettings>();

            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    options => options.UseNpgsqlConnection(database.ConnectionString),
                    new PostgreSqlStorageOptions
                    {
                        SchemaName = HangfireSchema.SchemaName,
                        PrepareSchemaIfNecessary = false,                 // tables come from `dotnet ef database update`
                        QueuePollInterval = TimeSpan.FromSeconds(jobs.PollIntervalSeconds),
                        UseSlidingInvisibilityTimeout = true,             // don't hold a connection while a job runs
                        InvisibilityTimeout = TimeSpan.FromMinutes(5),    // a crashed worker's jobs come back after this
                    });
        });

        if (AppSettings.RunsJobsFor(configuration))
        {
            // Registered before the job server, so it runs first: clear error if the tables are missing,
            // then the recurring job schedules.
            services.AddHostedService<JobsStartupService>();

            services.AddHangfireServer((sp, options) =>
            {
                var jobs = sp.GetRequiredService<JobsSettings>();
                options.ServerName = $"{Environment.MachineName}:{Environment.ProcessId}";
                options.WorkerCount = jobs.WorkerCount;
                options.Queues = jobs.Queues;
                options.ShutdownTimeout = TimeSpan.FromSeconds(jobs.ShutdownTimeoutSeconds);
            });
        }

        return services;
    }

    /// <summary>
    /// The dashboard at /jobs (roles all/api): open in Development; elsewhere only with
    /// JOBS_DASHBOARD_USERNAME and JOBS_DASHBOARD_PASSWORD (basic auth), otherwise not served.
    /// </summary>
    public static WebApplication MapJobsDashboard(this WebApplication app)
    {
        var jobs = app.Services.GetRequiredService<JobsSettings>();

        IDashboardAuthorizationFilter? authorization =
            app.Environment.IsDevelopment() ? new DashboardOpenFilter()
            : jobs.HasDashboardCredentials ? new DashboardBasicAuthFilter(jobs.DashboardUsername, jobs.DashboardPassword)
            : null;

        if (authorization is null)
        {
            return app;
        }

        app.MapHangfireDashboard(DashboardPath, new DashboardOptions
        {
            Authorization = [authorization],
            DashboardTitle = "DotnetApiTemplate jobs",
            DisplayStorageConnectionString = false,
            AppPath = "/",
        });

        return app;
    }
}
