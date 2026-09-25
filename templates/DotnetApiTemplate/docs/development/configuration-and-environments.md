# Configuration and environments

All configuration comes from environment variables, usually written in a `.env` file at the
repository root. `appsettings.json` only holds logging levels.

## Files

| File | Committed | Purpose |
| --- | --- | --- |
| `.env.example` | yes | Every variable, with local development defaults. The starting point. |
| `.env` | no | The active settings. The API, `dotnet ef` and Docker Compose read only this file. |
| `.env.dev`, `.env.stage`, `.env.prod` | no | Optional presets you create yourself and copy onto `.env`. |

`.gitignore` ignores `.env` and every `.env.*` except `.env.example`.

```bash
cp .env.example .env            # first run (Windows PowerShell: Copy-Item .env.example .env)

# Optional presets: keep one file per environment and switch by copying
cp .env.example .env.prod       # then set APP_ENV=prod and real values in it
cp .env.prod .env               # use it
```

## Environments: `APP_ENV`

`APP_ENV` is the one switch for the environment. It sets the ASP.NET Core environment at startup:

| `APP_ENV` | ASP.NET Core environment | What changes |
| --- | --- | --- |
| `dev` | Development | Swagger UI and `/openapi/v1.json`; email templates read from disk (live edits); exception type and message in error responses; EF SQL logged |
| `stage` | Staging | Production behavior (no Swagger, no exception details) |
| `prod` | Production | No Swagger, no exception details |

Any other value stops the app at startup: `APP_ENV must be one of: dev, stage, prod (was 'qa')`.

In code, check it with the injected `AppSettings` (`settings.IsDev`, `IsStage`, `IsProd`) or with
`IHostEnvironment` (`environment.IsDevelopment()`). Prefer feature flags in settings over
environment checks for anything that is really a configuration choice.

## Precedence

1. Variables already set in the process environment: Docker `environment:`, CI, your platform, your shell.
2. `.env` (loaded by `Common/Settings/EnvFile.cs` with [DotNetEnv](https://github.com/tonerdo/dotnet-env)).
3. Defaults in the settings classes.

So `.env` never overrides a real environment variable:

```bash
APP_ENV=stage dotnet run --project src/DotnetApiTemplate.Api     # Staging, whatever .env says
```

The API looks for `.env` in the current directory and its parents, so it works from the repository
root, from `src/...` and in tests.

## All variables

| Variable | Default | Used for |
| --- | --- | --- |
| `APP_ENV` | `dev` | Environment (see above) |
| `DB_HOST` | `localhost` | PostgreSQL host when the API runs outside Docker (inside Compose it's always `db`) |
| `DB_PORT` | `5432` | PostgreSQL port; also the host port Docker publishes it on (`54320` in this project) |
| `DB_NAME`, `DB_USER`, `DB_PASSWORD` | — (required) | Database name and credentials |
| `API_PORT` | `18080` | Host port of the API container |
| `CORS_ALLOWED_ORIGINS` | empty | Browser origins allowed to call the API, see [CORS](cors.md) |
| `JWT_SIGNING_KEY` | — (required, 32+ chars) | Signs access tokens, see [Authentication](authentication-and-authorization.md) |
| `JWT_ISSUER`, `JWT_AUDIENCE` | `DotnetApiTemplate` | Token issuer and audience |
| `JWT_EXPIRY_MINUTES` | `60` | Token lifetime (1–1440) |
| `EMAIL_HOST` | empty | SMTP host; empty only logs emails. See [Email](email.md) |
| `EMAIL_PORT` | `587` | SMTP port (in development: the Mailpit port, `21025` in this project) |
| `EMAIL_ENABLE_SSL` | `true` | TLS: implicit on 465, STARTTLS otherwise; `false` for Mailpit |
| `EMAIL_USERNAME`, `EMAIL_PASSWORD` | empty | SMTP login (skipped when the username is empty) |
| `EMAIL_FROM`, `EMAIL_FROM_NAME` | `no-reply@dotnetapitemplate.local`, `DotnetApiTemplate` | Sender address and display name |
| `MAILPIT_UI_PORT` | `28025` | Host port of the Mailpit inbox (Docker only) |

## Reading settings in code

### Typed settings (preferred)

A settings class works like a pydantic `BaseSettings`: each property names its variable, has a default,
and is validated when the app starts.

```csharp
// Common/Settings/DatabaseSettings.cs (shortened)
public sealed class DatabaseSettings : IEnvSettings   // found and registered automatically
{
    [ConfigurationKeyName("DB_HOST")]    // the variable name
    [Required]                           // validated at startup
    public string Host { get; init; } = "localhost";    // default

    [ConfigurationKeyName("DB_PORT")]
    [Range(1, 65535)]
    public int Port { get; init; } = 5432;              // "54320" is converted to int
}
```

Mark it with `IEnvSettings` and it's registered and validated automatically (`AddAllEnvSettings()` in
`Program.cs` finds every marked class). A class that isn't marked can still be registered by hand with
`services.AddEnvSettings<T>(configuration)`; registering a class twice is harmless.

Inject it directly, or as `IOptions<T>`:

```csharp
internal sealed class ReportService(DatabaseSettings db, AppSettings app) { ... }
```

Existing classes:

| Class | Location | Variables |
| --- | --- | --- |
| `AppSettings` | `Common/Settings/` | `APP_ENV` (+ `IsDev`, `IsStage`, `IsProd`) |
| `DatabaseSettings` | `Common/Settings/` | `DB_*` (+ `ConnectionString`) |
| `CorsSettings` | `Common/Cors/` | `CORS_ALLOWED_ORIGINS` (+ parsed `Origins`) |
| `JwtSettings` | `Infrastructure/Authentication/` | `JWT_*` |
| `EmailSettings` | `Infrastructure/Email/` | `EMAIL_*` |

### Raw values

For a quick one-off read, use DotNetEnv's typed getters (they read the environment, after `.env` is loaded):

```csharp
using DotNetEnv;

var host = Env.GetString("DB_HOST");
var port = Env.GetInt("DB_PORT", 5432);              // with a fallback
var enabled = Env.GetBool("FEATURE_X_ENABLED", false);
```

`IConfiguration` works too: `configuration["DB_HOST"]`. Raw reads aren't validated at startup, so use a
settings class for anything the app depends on.

## Adding a setting

Example: a `PAYMENTS_API_KEY` for a payment client.

1. **Add it to `.env.example`** with a safe default or placeholder and a comment. It's the only committed
   env file, so this is how the team learns about it. Add it to your own `.env` too.

   ```bash
   # Payment provider API key (test key for development)
   PAYMENTS_API_KEY=pk_test_change-me
   PAYMENTS_BASE_URL=https://sandbox.payments.example.com
   ```

2. **Create the settings class** next to the code that uses it:

   ```csharp
   // Infrastructure/ExternalServices/Payments/PaymentSettings.cs
   public sealed class PaymentSettings : IEnvSettings     // the marker: found and registered automatically
   {
       [ConfigurationKeyName("PAYMENTS_API_KEY")]
       [Required]
       public string ApiKey { get; init; } = string.Empty;

       [ConfigurationKeyName("PAYMENTS_BASE_URL")]
       [Required, Url]
       public string BaseUrl { get; init; } = string.Empty;
   }
   ```

3. **Inject it** (`PaymentSettings settings` or `IOptions<PaymentSettings>`) where needed. There's no
   registration line: `AddAllEnvSettings()` in `Program.cs` registers and validates every class marked
   `IEnvSettings`, in any folder.

4. **Document it** in the table above if other developers need to know.

Start the app without the variable and you get `PAYMENTS_API_KEY: The PAYMENTS_API_KEY field is required.`,
even before any code uses the class. A class with `[ConfigurationKeyName]` properties but without the
marker fails `SettingsDiscoveryTests`, so it can't be forgotten silently.

## Validation errors at startup

Every settings class is validated right after the app is built, and all problems are reported together
with the variable names:

```text
System.AggregateException: One or more errors occurred.
  (DB_PORT: The field DB_PORT must be between 1 and 65535.; DB_NAME: The DB_NAME field is required.)
  (JWT_SIGNING_KEY: The field JWT_SIGNING_KEY must be a string or array type with a minimum length of '32'.)
```

Fix the named variables in `.env` (or the environment) and start again.

## Secrets

- Never commit real secrets. `.env.example` holds only local-only defaults and placeholders.
- In production, either create `.env` on the server with real values, or (better) set the variables
  in your platform's environment or secret store; they always win over `.env`.
- Generate a JWT key with `openssl rand -base64 48`. Never run production with the development
  values from `.env.example` (development JWT key, `postgres`/`postgres`).
- Rotating `JWT_SIGNING_KEY` logs everyone out (existing tokens become invalid).
