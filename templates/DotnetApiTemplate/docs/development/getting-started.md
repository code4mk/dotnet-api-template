# Getting started

## Prerequisites

- .NET 10 SDK
- Docker Desktop (PostgreSQL and Mailpit run in containers)
- An IDE: Visual Studio 2026, JetBrains Rider or VS Code with C# Dev Kit

## First run

```bash
# 1. Settings: the only committed env file is the example
cp .env.example .env                      # Windows PowerShell: Copy-Item .env.example .env

# 2. PostgreSQL and the local email inbox
docker compose up -d db mailpit

# 3. Tables: migrations are applied manually, never at startup
dotnet build                              # restores packages: dotnet ef can't run on an unrestored project
dotnet tool restore
dotnet ef database update --project src/DotnetApiTemplate.Api

# 4. The API, with auto reload
dotnet watch --project src/DotnetApiTemplate.Api
```

On Windows, `./scripts/setup-local.ps1` does steps 1–2, restores tools and builds.

The database starts empty: register a user with `POST /api/users`, then log in with `POST /api/auth/login`.

| URL | What |
| --- | --- |
| `http://localhost:5080/` | Welcome message, environment, version |
| `http://localhost:5080/swagger` | Swagger UI ([how to use it](api-documentation.md)) |
| `http://localhost:5080/health` | Health check |
| `http://localhost:28025` | Mailpit: every email the API sends |
| `http://localhost:5080/jobs` | Background jobs dashboard ([guide](background-jobs.md)) |
| `src/DotnetApiTemplate.Api/DotnetApiTemplate.Api.http` | Sample requests for your IDE |

## Daily commands

| Task | Command |
| --- | --- |
| Run with auto reload | `dotnet watch --project src/DotnetApiTemplate.Api` |
| Run all tests | `dotnet test` |
| Build | `dotnet build` |
| Start / stop containers | `docker compose up -d db mailpit` / `docker compose down` |
| Everything in Docker (auto reload) | `docker compose --profile dev up --build` |
| Apply new migrations (after pulling) | `dotnet ef database update --project src/DotnetApiTemplate.Api` |
| Create a migration | `dotnet ef migrations add <Name> --project src/DotnetApiTemplate.Api --output-dir Data/Migrations` |
| Reset the local database | `docker compose down -v`, `docker compose up -d db`, then `database update` |

`dotnet watch` applies most code changes while the app runs (hot reload). When a change can't be applied
live (a changed method signature, a new DI registration) it asks to restart; set
`DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true` to restart without asking.

## Where to go next

| Task | Guide |
| --- | --- |
| Build a new feature end to end | [Adding a feature](adding-a-feature.md) |
| Add a setting, switch environments | [Configuration and environments](configuration-and-environments.md) |
| Change the schema or data | [Database and migrations](database-and-migrations.md) |
| Return or throw errors | [Errors and exceptions](errors-and-exceptions.md) |
| Protect endpoints, roles | [Authentication and authorization](authentication-and-authorization.md) |
| Send an email | [Email](email.md) |
| Run work in the background, schedule jobs | [Background jobs](background-jobs.md) |
| Connect a browser frontend | [CORS](cors.md) |
| Understand a JSON `400` | [JSON serialization](json-serialization.md) |
| Log, trace a request | [Logging and correlation ids](logging-and-correlation-ids.md) |
| Write tests | [Testing](testing.md) |
| Docker, CI, deploying | [Docker and deployment](docker-and-deployment.md) |
| Code rules, PR checklist | [Coding guidelines](coding-guidelines.md) |

All docs: [docs/README.md](../README.md).

## Troubleshooting

| Problem | Fix |
| --- | --- |
| `required variable DB_NAME is missing a value` (Compose) | Run Compose from the repository root, and create `.env` (`cp .env.example .env`) |
| `relation "Users" does not exist` | Apply migrations: `dotnet ef database update --project src/DotnetApiTemplate.Api` |
| Startup error listing variables (`JWT_SIGNING_KEY: ...`) | Fix the named variables in `.env`, see [Configuration](configuration-and-environments.md#validation-errors-at-startup) |
| `port is already allocated` | Another container uses the port: change `DB_PORT`, `API_PORT`, `EMAIL_PORT` or `MAILPIT_UI_PORT` in `.env` |
| `dotnet watch` fails with "address already in use" on 5080 | Another app uses port 5080: stop it, or change `applicationUrl` in `Properties/launchSettings.json` |
| `dotnet ef` not found | `dotnet tool restore` |
| `dotnet ef`: `Assets file '.../obj/project.assets.json' not found` / `Unable to retrieve project metadata` | The project isn't restored yet (fresh clone or new project, or `obj/` deleted): run `dotnet build` once, then the `dotnet ef` command again |
| `Hangfire's tables (schema "hangfire") don't exist` | Apply migrations: `dotnet ef database update --project src/DotnetApiTemplate.Api` |
| Emails don't show up in Mailpit | `docker compose up -d mailpit`, and check `EMAIL_HOST=localhost` and `EMAIL_PORT` in `.env` |
| Browser error `No 'Access-Control-Allow-Origin' header` | Add the frontend origin to `CORS_ALLOWED_ORIGINS`, see [CORS](cors.md) |
