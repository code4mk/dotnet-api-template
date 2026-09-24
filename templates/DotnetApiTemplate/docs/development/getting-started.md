# Getting started

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for PostgreSQL)
- An IDE: Visual Studio 2026, JetBrains Rider or VS Code with C# Dev Kit

## Environment files

All settings (database, JWT, email, seed data) live in dotenv files at the repository root:

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
| `JWT_SIGNING_KEY` | JWT signing key, at least 32 characters. Optional: `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRY_MINUTES`. |
| `EMAIL_HOST`, `EMAIL_PORT`, `EMAIL_ENABLE_SSL`, `EMAIL_USERNAME`, `EMAIL_PASSWORD`, `EMAIL_FROM`, `EMAIL_FROM_NAME` | SMTP. Development sends to Mailpit (`EMAIL_PORT` is its host port); empty `EMAIL_HOST` only logs emails. |
| `MAILPIT_UI_PORT` | Host port of the Mailpit inbox (`28025` in this project). |
| `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD` | Admin user seeded in Development. |

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
(`APP_ENV`, with `IsDev`/`IsStage`/`IsProd`), `DatabaseSettings`, `SeedSettings` in `Common/Settings/`,
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
cd docker
docker compose --env-file ../.env up -d db mailpit
cd ..

# 2. Run the API with auto reload
dotnet watch --project src/DotnetApiTemplate.Api
```

`dotnet watch` applies most code changes to the running app when you save (hot reload). If a change
can't be applied live, such as a changed method signature, it asks to restart; set
`DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true` to restart without asking. Use `dotnet run` for a run without watching.

The API listens on `http://localhost:5080`. In Development it creates the database, seeds an admin
user (`admin@example.com` / `Admin@12345`) and three sample products.

- OpenAPI document: `http://localhost:5080/openapi/v1.json`
- Health check: `http://localhost:5080/health`
- Sample requests: `src/DotnetApiTemplate.Api/DotnetApiTemplate.Api.http`

On Windows you can run `./scripts/setup-local.ps1` instead of step 1.

## Run everything in Docker

```bash
cd docker
docker compose --env-file ../.env up --build
```

The API is available at `http://localhost:18080` (`API_PORT` in `.env`). Inside Docker it always
connects to the database at `db:5432`, whatever `DB_HOST`/`DB_PORT` say. This builds the production image, so code changes
need `docker compose up --build`.

## Run everything in Docker with auto reload

```bash
cd docker
docker compose --env-file ../.env -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.dev.yml up --build
```

The API runs `dotnet watch` inside an SDK container with your source mounted, at `http://localhost:18080`.
Save a file under `src/` and the API reloads, with no image rebuild. Changes that can't be hot reloaded
restart the app automatically. The container keeps its own `bin/` and `obj/`, so it doesn't clash with
builds on your machine.

Only rebuild (`--build`) after changing `docker/Dockerfile.dev`. Stop with the same `--env-file` and `-f`
flags and `down` instead of `up --build`; add `-v` to also delete the database and NuGet cache volumes.

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

```bash
dotnet tool restore
dotnet ef migrations add InitialCreate --project src/DotnetApiTemplate.Api --output-dir Data/Migrations
```

Until the first migration exists, Development uses `EnsureCreated`. After that it applies migrations.

## Secrets

Never commit secrets. `.env`, `.env.dev` and `.env.prod` are in `.gitignore`; only `.env.example` is
committed, and it must keep placeholders or local-only defaults.

In production, either create `.env` on the server from `.env.example` with `APP_ENV=prod` and real
values (new database password, `JWT_SIGNING_KEY` from `openssl rand -base64 48`, your SMTP provider),
or set the same variables through your platform's environment or secret store, which always win over
`.env`. Never run production with the development values from `.env.example`.
