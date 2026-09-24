# Getting started

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for PostgreSQL)
- An IDE: Visual Studio 2026, JetBrains Rider or VS Code with C# Dev Kit

## Environment files

All settings (database, JWT, email) live in dotenv files at the repository root:

| File | Committed | Purpose |
| --- | --- | --- |
| `.env.example` | yes | Every variable with local development defaults. The starting point for `.env`. |
| `.env` | no | The active settings. The API and Docker Compose read only this file. |
| `.env.dev`, `.env.prod`, ... | no | Optional presets you create from `.env.example`. |

`.gitignore` ignores `.env` and every `.env.*` except `.env.example`, so only `.env.example` is in the
repository and in a new project. Create your settings from it before the first run:
`cp .env.example .env` (Windows PowerShell: `Copy-Item .env.example .env`). For presets, copy it to
`.env.dev` / `.env.prod` as well, set `APP_ENV` and the values, and switch by copying a preset onto `.env`,
e.g. `cp .env.prod .env` on a server.

| Variable | Used for |
| --- | --- |
| `APP_ENV` | `dev`, `stage` or `prod`: sets the ASP.NET Core environment (Development, Staging, Production). |
| `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` | PostgreSQL. The API builds its connection string from these. `DB_PORT` is also the host port Docker publishes PostgreSQL on (`54320` in this project, picked when it was created so it doesn't clash with other Postgres containers). |
| `API_PORT` | Host port for the API container (`18080` in this project). |
| `CORS_ALLOWED_ORIGINS` | Browser frontends on other origins allowed to call the API (comma-separated exact origins). Empty: none. See [CORS](#cors). |
| `JWT_SIGNING_KEY` | JWT signing key, at least 32 characters. Optional: `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRY_MINUTES`. |
| `EMAIL_HOST`, `EMAIL_PORT`, `EMAIL_ENABLE_SSL`, `EMAIL_USERNAME`, `EMAIL_PASSWORD`, `EMAIL_FROM`, `EMAIL_FROM_NAME` | SMTP. Development sends to Mailpit (`EMAIL_PORT` is its host port); empty `EMAIL_HOST` only logs emails. |
| `MAILPIT_UI_PORT` | Host port of the Mailpit inbox (`28025` in this project). |

The API loads `.env` at startup with [DotNetEnv](https://github.com/tonerdo/dotnet-env)
(`Common/Settings/EnvFile.cs`). Variables already set in the environment (Docker, CI, your shell) win
over the file, for example `APP_ENV=stage dotnet run ...`.
For a staging preset, copy `.env.example` to `.env.stage` and set `APP_ENV=stage`.

## Reading settings

**Typed settings (preferred)**, like pydantic `BaseSettings`: a class whose properties are bound from
environment variables, with defaults and validation.

```csharp
public sealed class DatabaseSettings
{
    [ConfigurationKeyName("DB_HOST")]   // the variable name, like Field(alias="DB_HOST")
    [Required]
    public string Host { get; init; } = "localhost";   // default when the variable is not set

    [ConfigurationKeyName("DB_PORT")]
    [Range(1, 65535)]
    public int Port { get; init; } = 5432;              // converted from the string value
}
```

Register it with `services.AddEnvSettings<DatabaseSettings>(configuration)` and inject it directly
(`DatabaseSettings settings`) or as `IOptions<DatabaseSettings>`. Existing classes: `AppSettings`
(`APP_ENV`, with `IsDev`/`IsStage`/`IsProd`), `DatabaseSettings` in `Common/Settings/`,
`JwtSettings` and `EmailSettings` in `Infrastructure/`.

All settings classes are validated when the app starts, and every problem is reported at once with
the variable name:

```text
DB_PORT: The field DB_PORT must be between 1 and 65535.; DB_NAME: The DB_NAME field is required.
JWT_SIGNING_KEY: The field JWT_SIGNING_KEY must be a string or array type with a minimum length of '32'.
```

**Raw values**, for a quick one-off read, with DotNetEnv's typed getters and an optional fallback:

```csharp
using DotNetEnv;

var host = Env.GetString("DB_HOST");
var port = Env.GetInt("DB_PORT", 5432);
var enabled = Env.GetBool("FEATURE_X_ENABLED", false);
```

`IConfiguration` works too: `configuration["DB_HOST"]`.

To add a setting: add the variable to `.env.example` (committed, so others see it) and your own `.env` files, then add a property with
`[ConfigurationKeyName("...")]` to a settings class (or create one and register it with `AddEnvSettings`).

## Run locally (API on your machine, database in Docker)

```bash
# 1. Create your settings (once) and start PostgreSQL and Mailpit
cp .env.example .env
docker compose up -d db mailpit

# 2. Create the tables (once, and after pulling new migrations)
dotnet tool restore
dotnet ef database update --project src/DotnetApiTemplate.Api

# 3. Run the API with auto reload
dotnet watch --project src/DotnetApiTemplate.Api
```

`dotnet watch` applies most code changes to the running app when you save (hot reload). If a change
can't be applied live, such as a changed method signature, it asks to restart; set
`DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true` to restart without asking. Use `dotnet run` for a run without watching.

The API listens on `http://localhost:5080`. It doesn't create tables or seed data: step 2 creates the
schema, and the database starts empty. Register a user with `POST /api/users`, then log in with
`POST /api/auth/login`.

- Swagger UI: `http://localhost:5080/swagger` (log in with `POST /api/auth/login`, then **Authorize** with
  the `accessToken`; endpoints with a lock need it)
- OpenAPI document: `http://localhost:5080/openapi/v1.json`
- Health check: `http://localhost:5080/health`
- Sample requests: `src/DotnetApiTemplate.Api/DotnetApiTemplate.Api.http`

On Windows you can run `./scripts/setup-local.ps1` instead of step 1.

## Docker Compose

There is one `docker-compose.yml` at the repository root. Run every command from the root; Compose
reads `.env` there automatically. Profiles choose what runs:

| Command | Runs |
| --- | --- |
| `docker compose up -d db mailpit` | PostgreSQL and Mailpit, for the API running on your machine (above) |
| `docker compose --profile dev up --build` | Everything in Docker, API with auto reload (`api-dev`) |
| `docker compose --profile prod up -d --build` | The production image of the API (`api`) with PostgreSQL |
| `docker compose --profile dev --profile prod down` | Stops everything; add `-v` to delete the database and NuGet cache volumes |

In both profiles the API is at `http://localhost:18080` (`API_PORT` in `.env`) and connects to the
database at `db:5432`, whatever `DB_HOST`/`DB_PORT` say. PostgreSQL and Mailpit are published on
`127.0.0.1` only, so they are reachable from this machine but not from the network.

**`dev` profile:** `api-dev` runs `dotnet watch` inside an SDK container with your source mounted and
sends email to Mailpit. Save a file under `src/` and the API reloads, with no image rebuild; changes
that can't be hot reloaded restart the app automatically. The container keeps its own `bin/` and
`obj/`, so it doesn't clash with builds on your machine. Only rebuild (`--build`) after changing
`docker/Dockerfile.dev`.

**`prod` profile:** `api` is built from `docker/Dockerfile` (the production image), so code changes need
`--build`. It sends email to `EMAIL_HOST` from `.env`.

## CORS

Needed only when a **browser** app on another origin calls the API, e.g. a Vite dev server on
`http://localhost:5173` or `https://app.example.com` calling `https://api.example.com`. Mobile apps,
other backends, Postman and a frontend served from the same domain don't need it.

Set the allowed origins in `.env`:

```bash
CORS_ALLOWED_ORIGINS=http://localhost:5173,http://localhost:3000     # development (the default in .env.example)
CORS_ALLOWED_ORIGINS=https://app.example.com                          # production: your real frontend
CORS_ALLOWED_ORIGINS=                                                 # no cross-origin browser access
```

- Only exact origins (`scheme://host[:port]`). Wildcards (`*`), paths and other schemes fail at
  startup with a clear message, so a typo can't open the API to every site.
- Allowed: `GET`, `POST`, `PUT`, `PATCH`, `DELETE`, and the headers `Authorization`, `Content-Type`,
  `Accept`, `X-Correlation-Id`. Frontend code can read `X-Correlation-Id` and `Location`.
- No credentials (cookies): send the JWT in the `Authorization` header.
- Preflight answers are cached by browsers for 10 minutes. Error responses carry CORS headers too, so
  the frontend can read the ProblemDetails body.

The policy lives in `Common/Cors/CorsExtensions.cs`.

## Email

Emails are typed classes rendered with [Scriban](https://github.com/scriban/scriban), wrapped in a
shared layout, CSS-inlined with [PreMailer.Net](https://github.com/milkshakesoftware/PreMailer.Net)
(Gmail and Outlook drop `<style>` blocks) and sent with [MailKit](https://github.com/jstedfast/MailKit)
as HTML plus a plain-text part.

```text
Infrastructure/Email/
├── IEmailService.cs, EmailService.cs    SendAsync(to, email): render + send (what features use)
├── EmailTemplate.cs                     base class for typed emails
├── EmailSettings.cs                     EMAIL_* settings
├── Rendering/                           Scriban (HTML-escaped) → layout → PreMailer, plain-text part
├── Sending/                             MailKit (SMTP) or logging when EMAIL_HOST is empty
└── Layout/_layout.html.scriban, email.css
Features/Users/Emails/
├── WelcomeEmail.cs                      model + subject
└── WelcomeEmail.html.scriban            body (the layout adds header and footer)
```

**See the emails locally:** Mailpit catches everything the API sends. Open the inbox at
`http://localhost:28025` (`MAILPIT_UI_PORT`). Create a user (`POST /api/users`) to get the sample
welcome email.

**Add an email:**

1. Create the model next to the feature, e.g. `Features/Auth/Emails/PasswordResetEmail.cs`:

   ```csharp
   public sealed class PasswordResetEmail(string fullName, string resetUrl) : EmailTemplate
   {
       public string FullName { get; } = fullName;
       public string ResetUrl { get; } = resetUrl;
       public override string Subject => "Reset your password";
   }
   ```

2. Add `PasswordResetEmail.html.scriban` next to it. Properties are available in snake_case, plus
   `app_name` and `year`:

   ```html
   <h1>Hi {{ full_name }},</h1>
   <p><a class="button" href="{{ reset_url }}">Reset password</a></p>
   ```

   Optionally add `PasswordResetEmail.txt.scriban` for the plain-text part; otherwise it is generated
   from the HTML.

3. Send it: `await emailService.SendAsync(user.Email, new PasswordResetEmail(user.FullName, url), ct);`

Rules:

- Every `{{ value }}` is HTML-escaped automatically. Use `{{ value | raw }}` only for HTML you control.
- Templates are code: never render a template that comes from a user or the database.
- Template file names must be unique (they are embedded by file name). Styles go in `email.css`
  using classes; they are inlined when the email is rendered.
- In Development templates are read from disk, so edits show up in the next email without a restart.
- Decide per email whether a failed send may fail the request. The welcome email logs the error and
  lets the sign-up succeed (see `UserService`).

## Tests

```bash
dotnet test
```

Unit tests use the EF Core in-memory provider. Integration tests start the API in memory with
`WebApplicationFactory`, an in-memory database and a fake authentication scheme.

## Database migrations

The API **never** creates or changes the database schema at startup, and it seeds no data. You apply
migrations yourself, so every schema change is reviewed and deliberate. The project ships with an
`InitialCreate` migration (users and products tables).

```bash
dotnet tool restore                                           # once: installs dotnet-ef

# Apply all pending migrations to the database in .env (DB_*)
dotnet ef database update --project src/DotnetApiTemplate.Api

# After changing an entity or configuration: create a migration, review it, then apply it
dotnet ef migrations add AddProductSku --project src/DotnetApiTemplate.Api --output-dir Data/Migrations
dotnet ef database update --project src/DotnetApiTemplate.Api

# Undo the last migration that is not applied yet
dotnet ef migrations remove --project src/DotnetApiTemplate.Api

# Production: generate an idempotent SQL script, review it and run it in your deployment
dotnet ef migrations script --idempotent --project src/DotnetApiTemplate.Api --output migrations.sql
```

`dotnet ef` reads the database settings from `.env` like the API does. On Windows you can also use
`./scripts/add-migration.ps1` and `./scripts/update-database.ps1`.

**Starting with an empty database:** register a user with `POST /api/users` and log in with
`POST /api/auth/login`. To make someone an admin (needed only for `DELETE /api/users/{id}`), update
the row directly: `UPDATE "Users" SET "Role" = 'Admin' WHERE "Email" = 'you@example.com';`

## Secrets

Never commit secrets. `.env`, `.env.dev` and `.env.prod` are in `.gitignore`; only `.env.example` is
committed, and it must keep placeholders or local-only defaults.

In production, either create `.env` on the server from `.env.example` with `APP_ENV=prod` and real
values (new database password, `JWT_SIGNING_KEY` from `openssl rand -base64 48`, your SMTP provider),
or set the same variables through your platform's environment or secret store, which always win over
`.env`. Never run production with the development values from `.env.example`.
