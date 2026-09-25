# ADR-0004: Run Background Jobs with Hangfire and PostgreSQL

We will run background, delayed and recurring jobs with Hangfire, stored in the application's PostgreSQL
database, and run the API and the job worker as two processes from the same build.

| Field | Value |
| --- | --- |
| Status | Accepted |
| Date | 2026-09-25 |
| Scope | Work that must not run in the request, delayed work, and scheduled (cron) work |
| Code | `Infrastructure/Jobs/`, jobs next to each feature (`Features/<Feature>/Jobs/`) |

## Context

Some work doesn't belong in an HTTP request: sending email, calling slow external services, generating reports,
cleaning up data on a schedule. Doing it in the request makes responses slow, and a failure either fails the
request or is lost.

Forces:

- **Durable.** Queued work must survive restarts and deployments.
- **Retries.** Transient failures (SMTP down, a provider timing out) must be retried automatically.
- **Delayed and recurring work** (reminders, nightly reports) without a separate cron system.
- **Many instances.** The API may run as several containers; each job and each schedule must run once.
- **Operable.** Someone on call must see what's queued, running and failing, and retry failed work.
- **Little extra infrastructure.** Every project already has PostgreSQL; avoid a mandatory broker or cache.
- **Simple deployment.** One container per instance (supervisor inside), or a VM with supervisor; no
  cloud-specific services.
- **Schema changes only through migrations** (the project rule: nothing changes the database at startup).

## Options considered

| Option | Verdict |
| --- | --- |
| **Hangfire + PostgreSQL** (`Hangfire.PostgreSql`) | ✅ Chosen. Fire-and-forget, delayed, recurring and continuation jobs, retries, dashboard, distributed workers; uses the existing database |
| Hangfire + Redis | Faster, but another service to run and persist; the official Redis storage is a paid Pro package. Possible later without changing job code |
| Quartz.NET | Strong scheduler with clustering, but no dashboard and clumsier for one-off "do this now" jobs |
| TickerQ | Modern and EF Core based, but young with a small community |
| MassTransit / Wolverine | Real messaging (sagas, outbox); needs a broker and solves a bigger problem than jobs |
| `BackgroundService` / `Channel<T>` | No dependency, but not durable: work is lost on restart, no retries, no dashboard |
| Cloud queues (SQS, Service Bus) + functions | Platform lock-in; contradicts cloud-neutral deployment |

## Decision

We use Hangfire with PostgreSQL storage.

1. **Storage in the app database**, schema `hangfire`. Its tables are created by the EF Core migration
   `AddHangfireSchema` (the same SQL as the package's installer), never by Hangfire at startup
   (`PrepareSchemaIfNecessary = false`). A test fails when a package update adds a schema version that has no
   migration yet.
2. **Roles from one build** (`APP_ROLE`): `all` (API and jobs in one process, the local default), `api` (HTTP,
   only enqueues), `worker` (runs jobs and triggers delayed and recurring ones; only `/health` over HTTP). There's
   no separate scheduler process: every worker also schedules, and database locks make each schedule fire once.
3. **Production: one container, two processes** under supervisor (`api` on 8080, `worker` on 8081). If a
   process can't stay up, the container stops so the platform restarts it. The same image can also run each role
   in its own container when API and workers need to scale separately. VMs use the same programs with supervisor.
4. **Tuned for many instances:** sliding invisibility timeout (no connection held while a job runs), a
   configurable poll interval, worker count and queues, graceful shutdown, and `DB_MAX_POOL_SIZE` to control
   connections.
5. **Jobs are small classes in their feature**, activated from DI per run. Rules: simple arguments (ids),
   idempotent, honor the `CancellationToken`, throw to retry and return to finish, don't rename queued job types.
6. **Features depend only on `IBackgroundJobClient`**, never on Hangfire internals or storage.
7. **`JOBS_ENABLED=false` switches Hangfire off** for projects that don't need a queue yet: enqueued jobs then
   run inline in the request, delayed jobs are rejected, schedules don't run. Turning it on later needs no code
   change, because the tables come with the migrations.
8. **Dashboard at `/jobs`:** open in Development; elsewhere only with basic auth credentials from the environment
   (a browser page can't carry the API's bearer token), otherwise not served.
9. **Recurring jobs are registered in one place** (`Infrastructure/Jobs/RecurringJobs.cs`), in UTC.

## Consequences

**Positive**

- Durable jobs with retries, schedules and a dashboard, with no new infrastructure.
- Horizontal scaling: more workers or containers share one queue; each job and schedule runs once.
- Requests stay fast; side effects (email) no longer fail or slow down the request.
- The database schema stays under migration control, like the rest of the model.
- Projects can start without jobs (`JOBS_ENABLED=false`) and enable them later with one setting.

**Negative**

- Jobs add load and connections to the application database.
- Job type and method names are stored with each job: renaming or moving a job class while jobs of the old name
  are queued breaks them.
- Hangfire serializes with Newtonsoft.Json; its old transitive version has to be pinned to a patched one.
- Pickup latency is the poll interval (5 s by default), not instant (PostgreSQL LISTEN/NOTIFY is off because it
  doesn't work through PgBouncer's transaction pooling).
- Supervisor in one container scales API and worker together and shares CPU and memory between them.
- A Hangfire.PostgreSql schema upgrade needs a small manual migration.

**Risks and mitigations**

| Risk | Mitigation |
| --- | --- |
| Duplicate side effects when a job is retried or re-queued after a worker stops | Idempotent jobs (documented rule); jobs load current data and check before acting |
| Too many database connections with many instances | `DB_MAX_POOL_SIZE`, a pooler (PgBouncer, RDS Proxy), no connection held during jobs |
| Queued jobs break after a rename | Keep the old class until the queue drains; failed jobs are visible in the dashboard |
| Dashboard exposed publicly | Off by default outside Development; basic auth over HTTPS only; restrict `/jobs` at the proxy |
| Workers not running (misconfigured role or queue) | Startup logs the role; the dashboard's Servers page shows workers and their queues |
| Throughput outgrows PostgreSQL storage | Switch Hangfire to Redis storage; job code is unchanged |
| Hangfire's tables missing | The worker stops at startup with a clear "run the migrations" message |

## References

- [Developer guide: Background jobs](../development/background-jobs.md)
- [Docker and deployment](../development/docker-and-deployment.md) (supervisor image, VM setup)
- [Hangfire documentation](https://docs.hangfire.io/) and [licensing](https://www.hangfire.io/licensing.html)
- [Hangfire.PostgreSql](https://github.com/frankhommers/Hangfire.PostgreSql)
- [Supervisor](http://supervisord.org/)
- [ADR-0003: Send Email with Scriban Templates, PreMailer.Net and MailKit](0003-send-email-with-scriban-premailer-mailkit.md)
