# Background jobs

[Hangfire](https://www.hangfire.io/) with PostgreSQL storage: fire-and-forget, delayed and recurring (cron)
jobs, automatic retries, a dashboard, and any number of worker processes sharing one queue.

```text
API process (APP_ROLE=api)            PostgreSQL, schema "hangfire"          worker process (APP_ROLE=worker)
jobs.Enqueue<SendWelcomeEmailJob>()  ──INSERT──▶  jobs, queues, locks  ◀──claim──  runs jobs, retries failures,
                                                                                    triggers delayed + recurring jobs
```

- Jobs are stored in the database, so they survive restarts and deployments.
- Workers claim jobs with row locks: each job runs once, whatever the number of workers.
- The worker is also the scheduler: every job server triggers delayed and recurring jobs, and a database
  lock makes each recurring job fire once. There's no separate scheduler process.

## Roles: `APP_ROLE`

The same build runs as one of three roles:

| `APP_ROLE` | HTTP | Job server | Used |
| --- | --- | --- | --- |
| `all` (default) | API + Swagger + `/jobs` | ✅ | local development: one `dotnet watch`, jobs just work |
| `api` | API + `/jobs` | ❌ (only enqueues) | production API process |
| `worker` | only `/health` | ✅ | production worker process |

In production the Docker image runs both processes in **one container with supervisor**
(`docker/supervisor/supervisord.conf`): `api` on port 8080, `worker` on 8081. On a VM, use
`deploy/supervisor/dotnetapitemplate.conf`. See [Docker and deployment](docker-and-deployment.md).

## Turning jobs off: `JOBS_ENABLED`

`JOBS_ENABLED=true` (default) is everything described here. `JOBS_ENABLED=false` switches Hangfire off
completely, for projects or environments that don't need a queue yet:

| | `JOBS_ENABLED=true` | `JOBS_ENABLED=false` |
| --- | --- | --- |
| `jobs.Enqueue<T>(...)` | stored, run by a worker, retried on failure | **runs immediately, in the request**, in its own DI scope; a failure is logged, not retried, and the request still succeeds |
| `jobs.Schedule<T>(...)` (delayed) | runs at the given time | rejected with `NotSupportedException` (it must not run early) |
| Recurring jobs | scheduled | don't run |
| Dashboard `/jobs` | served | `404` |
| `APP_ROLE=worker` process | runs jobs | stays up with only `/health` and logs that it has nothing to do |
| `hangfire` tables | used | exist (from the migrations), unused |

Startup logs a warning while jobs are off. Features don't change between the modes: they always use
`IBackgroundJobClient`. **Turning jobs on later is only `JOBS_ENABLED=true`**; the tables are already there.

Keep `true` in production whenever emails or other work must not be lost: with `false`, a failed job is gone.

## Setup

Hangfire's tables are created by the migrations, like everything else (never at startup):

```bash
dotnet ef database update --project src/DotnetApiTemplate.Api   # also creates schema "hangfire"
```

A job server started before that stops with: `Hangfire's tables (schema "hangfire") don't exist. Apply the
migrations first: dotnet ef database update ...`.

## Enqueue a job

Inject `IBackgroundJobClient` and enqueue a call to a job class:

```csharp
internal sealed class UserService(AppDbContext db, IPasswordHasher<User> passwordHasher, IBackgroundJobClient jobs)
{
    public async Task<Result<UserResponse>> CreateAsync(...)
    {
        ...
        await db.SaveChangesAsync(cancellationToken);

        jobs.Enqueue<SendWelcomeEmailJob>(job => job.ExecuteAsync(user.Id, CancellationToken.None));
        return user.ToResponse();
    }
}
```

Other kinds:

```csharp
jobs.Schedule<SendReminderJob>(job => job.ExecuteAsync(orderId, CancellationToken.None), TimeSpan.FromHours(24));   // delayed
var id = jobs.Enqueue<ExportJob>(job => job.ExecuteAsync(exportId, CancellationToken.None));
jobs.ContinueJobWith<NotifyExportReadyJob>(id, job => job.ExecuteAsync(exportId, CancellationToken.None));       // after another job
```

`CancellationToken.None` is a placeholder: Hangfire passes the real token (cancelled when the worker stops).

**Enqueue after saving.** Enqueue once the database change is committed (after `SaveChangesAsync`), so the
job never runs for data that was rolled back.

## Write a job

Jobs live in the feature they belong to: `Features/<Feature>/Jobs/<Name>Job.cs`.

```csharp
// Features/Users/Jobs/SendWelcomeEmailJob.cs
[Queue(JobQueues.Emails)]                 // which queue (default: "default")
[AutomaticRetry(Attempts = 5)]            // retries with increasing delays, then "Failed" in the dashboard
public sealed class SendWelcomeEmailJob(AppDbContext db, IEmailService emailService, ILogger<SendWelcomeEmailJob> logger)
{
    public async Task ExecuteAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("User {UserId} no longer exists; welcome email skipped", userId);
            return;                       // can never succeed: finish instead of retrying
        }

        await emailService.SendAsync(user.Email, new WelcomeEmail(user.FullName, user.Email), cancellationToken);
    }
}
```

Nothing to register: Hangfire creates the job class with its dependencies from DI, in its own scope
(its own `AppDbContext`), for every run.

### The rules

1. **Small, simple arguments.** Pass ids and small values, not entities or DTOs. Arguments are stored as JSON
   when the job is enqueued; the job loads current data when it runs.
2. **Idempotent.** A job can run more than once: after a retry, or when a worker stops mid-job and the job is
   re-queued. Running it twice must be harmless (check before acting, use unique keys, "if not already sent").
3. **Honor the `CancellationToken`.** Pass it on (EF, HTTP, email). When a worker stops, running jobs get
   `JOBS_SHUTDOWN_TIMEOUT_SECONDS` to finish, then are cancelled and re-queued.
4. **Don't rename or move queued job classes and methods carelessly.** The job's type and method name are
   stored. Deploying a rename while jobs of the old name are queued makes them fail. Keep the old class until
   the queue has drained, or rename when it's empty.
5. **Throw to retry, return to finish.** An exception means "failed, retry later"; returning means done. A job
   that can never succeed (deleted user) should return, not throw.
6. **Keep jobs short.** Split big work into many small jobs (one per item) instead of one job running for hours.

### Queues

Queues let urgent work skip ahead and let you dedicate workers:

| Queue | For |
| --- | --- |
| `default` | everything without a `[Queue]` attribute |
| `emails` | outgoing email |

`JOBS_QUEUES=default,emails` lists the queues a worker processes, highest priority first. Add a queue: a constant
in `JobQueues` (lowercase, letters/digits/underscores), `[Queue(JobQueues.Reports)]` on the job, and add it to
`JOBS_QUEUES`. **A queue no worker listens to is never processed.**

## Recurring jobs

All schedules are in one place, `Infrastructure/Jobs/RecurringJobs.cs`:

```csharp
jobs.AddOrUpdate<LowStockReportJob>(
    "products.low-stock-report",          // stable id: the dashboard shows it; changing it creates a new schedule
    JobQueues.Default,
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Daily(6),                        // "0 6 * * *": every day at 06:00
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

| Schedule | Cron |
| --- | --- |
| Every minute | `Cron.Minutely()` or `* * * * *` |
| Every 15 minutes | `*/15 * * * *` |
| Hourly at :30 | `Cron.Hourly(30)` |
| Daily at 06:00 | `Cron.Daily(6)` |
| Mondays at 08:00 | `Cron.Weekly(DayOfWeek.Monday, 8)` |
| 1st of the month at 00:00 | `Cron.Monthly()` |

- Schedules are registered when a job server starts. Each worker registers them; each still fires once.
- **Removing a line doesn't delete the schedule** from the database. Delete it once with
  `jobs.RemoveIfExists("products.low-stock-report")` (or in the dashboard), then remove the line.
- A recurring job that must never overlap with itself gets `[DisableConcurrentExecution(timeoutInSeconds: 600)]`
  (see `LowStockReportJob`): a second run waits for the first, across all workers.
- Use UTC. A job at 02:30 local time can run twice or not at all when daylight saving time changes.

## Dashboard: `/jobs`

Queued, running, scheduled, succeeded and failed jobs, retries, recurring schedules and servers.
Failed jobs show the exception and can be retried with one click.

| Environment | Access |
| --- | --- |
| Development | open: `http://localhost:5080/jobs` |
| Staging / production | only when `JOBS_DASHBOARD_USERNAME` and `JOBS_DASHBOARD_PASSWORD` are set: HTTP basic auth. Not served otherwise |

The dashboard is a browser page, so the API's JWT can't protect it: that's why it uses basic auth. Serve it
over HTTPS only (basic auth sends the password with every request), use a long random password, and
consider restricting `/jobs` to your network at the reverse proxy.

## Configuration

| Variable | Default | |
| --- | --- | --- |
| `JOBS_ENABLED` | `true` | `false` switches Hangfire off: jobs run inline in the request (above) |
| `APP_ROLE` | `all` | `all` / `api` / `worker` (above) |
| `JOBS_WORKER_COUNT` | `10` | Jobs run in parallel by one worker process |
| `JOBS_QUEUES` | `default,emails` | Queues this worker processes, highest priority first |
| `JOBS_POLL_INTERVAL_SECONDS` | `5` | How often idle workers check for new jobs (the delay before a new job starts) |
| `JOBS_SHUTDOWN_TIMEOUT_SECONDS` | `60` | Grace time for running jobs when a worker stops; keep it below supervisor's `stopwaitsecs` (75) |
| `JOBS_DASHBOARD_USERNAME` / `_PASSWORD` | empty | Dashboard login outside Development |
| `DB_MAX_POOL_SIZE` | `100` | Database connections per process (see Scaling) |

Code: `Infrastructure/Jobs/` (`JobsExtensions` registration, `JobsSettings`, `RecurringJobs`, `HangfireSchema`,
dashboard auth).

## Scaling

- **More throughput in one container:** raise `JOBS_WORKER_COUNT` (parallel jobs per worker process).
- **More containers:** run more copies of the image. Every container's worker joins the same queue; each job
  still runs once. With many containers, the API processes can also be separated from the workers: the same
  image with `--entrypoint dotnet` and `APP_ROLE=api` or `APP_ROLE=worker` (no supervisor).
- **Database connections:** every process has a pool of up to `DB_MAX_POOL_SIZE` connections. Keep
  containers × 2 processes × `DB_MAX_POOL_SIZE` below PostgreSQL's `max_connections`, lower the pool size, or
  put PgBouncer / RDS Proxy in front.
- Workers don't hold a database connection while a job runs, and a crashed worker's jobs are picked up
  again after 5 minutes (invisibility timeout).
- PostgreSQL storage comfortably handles hundreds to a few thousand jobs per minute. Beyond that, Hangfire
  can switch to Redis storage without changing job code.

## Upgrading Hangfire.PostgreSql

The Hangfire schema is created by the migration `AddHangfireSchema`, which runs the package's own install
scripts for versions 3–23 (`HangfireSchema.InstallSql`). When an update of `Hangfire.PostgreSql` adds a schema
version, `HangfireSchemaTests` fails with the exact fix:

```bash
dotnet ef migrations add UpdateHangfireSchema --project src/DotnetApiTemplate.Api --output-dir Data/Migrations
```

```csharp
protected override void Up(MigrationBuilder migrationBuilder) =>
    migrationBuilder.Sql(HangfireSchema.InstallSql(fromVersion: 24, toVersion: 24));   // the new version(s)

protected override void Down(MigrationBuilder migrationBuilder) { }                    // not reversible
```

Then raise `HangfireSchema.MigratedVersion`.

## Testing

- **Services:** `FakeBackgroundJobClient` (unit tests) records enqueued jobs. Assert the job type and arguments:

  ```csharp
  var job = Assert.Single(_jobs.Jobs);
  Assert.Equal(typeof(SendWelcomeEmailJob), job.Type);
  Assert.Equal(userId, job.Args[0]);
  ```

- **Jobs:** create the job class directly with an in-memory `AppDbContext` and fakes, and call `ExecuteAsync`
  (see `SendWelcomeEmailJobTests`).
- **Integration tests:** the API runs as `APP_ROLE=api` with a fake job client. Run what an endpoint enqueued
  like a worker would, with the app's real services:

  ```csharp
  await factory.Jobs.RunAsync(factory.Services, job => job.Type == typeof(SendWelcomeEmailJob));
  ```

## Troubleshooting

| Problem | Fix |
| --- | --- |
| Startup: `Hangfire's tables (schema "hangfire") don't exist` | `dotnet ef database update --project src/DotnetApiTemplate.Api` |
| Jobs stay in "Enqueued" | No worker runs that queue: check `APP_ROLE` (all/worker) and that the queue is in `JOBS_QUEUES`; the dashboard's **Servers** page lists running workers and their queues |
| A job keeps failing | Dashboard → **Failed**: the exception and stack trace; fix, then **Requeue** |
| Jobs fail after a deployment with "type not found" | A queued job's class or method was renamed/moved (rule 4) |
| `/jobs` returns 404 outside Development | Set `JOBS_DASHBOARD_USERNAME` and `JOBS_DASHBOARD_PASSWORD` |
| A container keeps restarting | A process couldn't stay up and supervisor stopped the container: `docker logs` shows the reason (often missing migrations or settings) |
