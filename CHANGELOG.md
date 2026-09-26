# Changelog

All notable changes to the template are recorded here.
Versions follow [Semantic Versioning](https://semver.org/): breaking structure changes bump the major version.

## [1.0.0] - 2026-09-27

First stable release. Projects created from 0.0.1 differ in structure and settings; see
[Upgrading from 0.0.1](#upgrading-from-001).

### Added

- **Configuration from environment variables:** `.env` at the repository root (the API, `dotnet ef` and Docker
  Compose read it; only `.env.example` is committed). Typed settings classes like pydantic `BaseSettings`
  (`[ConfigurationKeyName]`, defaults, data annotations), marked `IEnvSettings` and discovered automatically,
  all validated at startup with every invalid variable reported by name. Raw reads with DotNetEnv (`Env.GetString`).
- **`APP_ENV`** (`dev` / `stage` / `prod`) sets the ASP.NET Core environment.
- **Email:** typed emails (`EmailTemplate`), Scriban templates with automatic HTML escaping, a shared layout,
  CSS inlining with PreMailer.Net, plain-text part, sending with MailKit (logged when `EMAIL_HOST` is empty),
  Mailpit inbox for local development, sample welcome email ([ADR-0003](templates/DotnetApiTemplate/docs/adr/0003-send-email-with-scriban-premailer-mailkit.md)).
- **Background jobs:** Hangfire with PostgreSQL (schema `hangfire`) for queued, delayed and recurring jobs with
  retries; `/jobs` dashboard (open in Development, basic auth elsewhere); `APP_ROLE` (`all` / `api` / `worker`);
  `JOBS_ENABLED` to switch Hangfire off (jobs then run inline); welcome email and a sample recurring job
  ([ADR-0004](templates/DotnetApiTemplate/docs/adr/0004-run-background-jobs-with-hangfire-and-postgresql.md)).
- **Production image with supervisor:** API (8080) and job worker (8081) in one container, health check on both,
  the container stops if a process can't stay up; supervisor config for VMs (`deploy/supervisor/`).
- **One `docker-compose.yml`** at the root with profiles: `db` + `mailpit` for local development, `dev` (API with
  `dotnet watch` in Docker), `prod` (production image). PostgreSQL and Mailpit listen on `127.0.0.1` only.
- **Per-project ports** picked at `dotnet new` time (`DB_PORT`, `API_PORT`, `EMAIL_PORT`, `MAILPIT_UI_PORT`),
  so several projects run side by side.
- **Swagger UI** at `/swagger` (Development) on the built-in OpenAPI document, with JWT **Authorize** and a lock
  only on endpoints that need a token.
- **Global exception handling:** `ExceptionMapping` turns bad JSON, missing bodies, unique/foreign-key and
  concurrency violations, aborted requests and unknown errors into the right status and a stable error code;
  client errors logged as one line, real failures with the stack trace; `correlationId` in every error response.
- **Application exceptions** sharing the error catalog: `RequestValidationException`, `UnauthorizedException`,
  `ForbiddenException`, `NotFoundException`, `ConflictException`, `BusinessRuleException` (422),
  `ExternalServiceException` (502/503), and `Error.ToException()`.
- **Production JSON settings** (`JsonDefaults`): strict reading (no numbers as strings, enum numbers, duplicate
  properties, comments), readable Unicode, dates always ISO 8601 UTC with `Z`, JSON path in `invalid_json` errors.
- **CORS** for exact origins from `CORS_ALLOWED_ORIGINS`, validated at startup (no wildcards).
- **`GET /`** with the API name, environment, version and useful links.
- **Optional feature auto-discovery:** `dotnet new ... --auto-discovery true` maps every `IEndpoints` class and
  registers every `XService : IXService`; manual registration stays the default. Route snapshot and wiring tests
  guard both modes.
- **`InitialCreate` migration** shipped with the template; the Hangfire schema comes as a migration too.
- **NuGet lock files** (`packages.lock.json`) with locked restores in CI and the Docker build.
- **Dependency injection validated at startup** (`ValidateOnBuild`, `ValidateScopes`) in every environment.
- **Developer guides**, one per topic (`docs/README.md`): configuration and environments, adding a feature,
  database and migrations (including data migrations), request data (route, query strings, headers, JSON
  bodies, PATCH, file uploads), response shaping (the equivalent of Laravel API Resources), errors and
  exceptions, authentication, email, background jobs, CORS, JSON, Swagger, logging and correlation ids,
  testing, Docker and deployment, packages and dependencies.
- **ADRs** for email ([ADR-0003](templates/DotnetApiTemplate/docs/adr/0003-send-email-with-scriban-premailer-mailkit.md))
  and background jobs ([ADR-0004](templates/DotnetApiTemplate/docs/adr/0004-run-background-jobs-with-hangfire-and-postgresql.md)).

### Changed

- **Template short name is `dotnet-api-template`** (was `code4mk-api`); display name
  "Code4mk ASP.NET Core API Template".
- **The API never changes the database at startup:** no `EnsureCreated`, no automatic migrations; apply them with
  `dotnet ef database update`.
- Endpoint classes implement `IEndpoints` (were static `MapXEndpoints` extension methods).
- `JwtOptions` / `EmailOptions` are `JwtSettings` / `EmailSettings`, bound from `JWT_*` / `EMAIL_*`.
- The welcome email is sent from a background job (sign-up no longer waits for SMTP).
- ASP.NET Core and EF Core packages and `dotnet-ef` 10.0.12, Npgsql 10.0.3; central transitive pinning enabled.
- Template repository workflows (`template-ci`, `publish`) run manually; `template-ci` tests both wiring modes.

### Removed

- Startup seeding of an admin user and sample products (`SEED_*` settings). Register with `POST /api/users`;
  promote an admin with SQL (see the authentication guide).
- `docker/.env.example` and the Compose files under `docker/` (replaced by the root `.env.example` and
  `docker-compose.yml`).
- `appsettings.*.json` configuration values (only logging remains there).

### Fixed

- The first `dotnet ef database update` on an empty database no longer logs a misleading
  "Failed executing DbCommand" for the missing history table.
- `dotnet watch` no longer fails reading `obj\Debug/.../staticwebassets.development.json` on macOS/Linux.
- First-run docs build the project before `dotnet ef` (it doesn't restore packages itself).

### Security

- `Microsoft.OpenApi` 2.0.0 → 2.12.0 (GHSA-v5pm-xwqc-g5wc).
- `Newtonsoft.Json` pinned to 13.0.4: Hangfire brings 11.0.1 (GHSA-5crp-9r3c-p9vr).
- Hangfire dashboard basic auth with constant-time comparison; not served outside Development without credentials.

### Upgrading from 0.0.1

1.0.0 is a new baseline, not an in-place upgrade: create new projects with 1.0.0. For an existing 0.0.1 project,
the main steps are:

1. Update the template: `dotnet new install Code4mk.MinimalApi.Template::1.0.0` (the command is now
   `dotnet new dotnet-api-template`).
2. Move configuration to a root `.env` (from `.env.example`): database, JWT and email settings are environment
   variables now (`DB_*`, `JWT_*`, `EMAIL_*`), not `appsettings.*.json`.
3. Create an initial migration if the project has none, and apply migrations with `dotnet ef database update`
   before starting the API: it no longer creates or seeds the database.
4. Copy the features you want from a freshly generated 1.0.0 project (for example `Infrastructure/Jobs`,
   `Infrastructure/Email`, `Common/Settings`) and follow the matching developer guide.

## [0.0.1] - 2026-09-24

First preview.

### Added

- Minimal API solution with feature folders and a service layer (short name `code4mk-api`).
- Sample features: Auth (JWT login), Users, Products.
- `Result<T>` pattern, ProblemDetails errors, .NET 10 built-in validation.
- EF Core with PostgreSQL, development seed data.
- Correlation id middleware, health checks, OpenAPI.
- Docker and Docker Compose, GitHub Actions for build/test and image publishing.
- Unit tests (EF Core in-memory) and integration tests (`WebApplicationFactory`).
- ADRs, architecture overview, API conventions and developer guides.
