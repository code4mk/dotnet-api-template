# Getting started

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for PostgreSQL)
- An IDE: Visual Studio 2026, JetBrains Rider or VS Code with C# Dev Kit

## Run locally (API on your machine, database in Docker)

```bash
# 1. Start PostgreSQL
cd docker
cp .env.example .env
docker compose up -d db
cd ..

# 2. Run the API with auto reload
dotnet watch --project src/DotnetApiTemplate.Api
```

`dotnet watch` applies most code changes to the running app when you save (hot reload). If a change
can't be applied live, such as a changed method signature, it asks to restart; set
`DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true` to restart without asking. Use `dotnet run` for a run without watching.

PostgreSQL is published on the host port `DB_PORT` from `docker/.env` (`54320` in this project, picked
when the project was created so it doesn't clash with other Postgres containers). To use another port,
change `DB_PORT` and the `Port=` in `appsettings.Development.json` together.

The API listens on `http://localhost:5080`. In Development it creates the database, seeds an admin
user (`admin@example.com` / `Admin@12345`) and three sample products.

- OpenAPI document: `http://localhost:5080/openapi/v1.json`
- Health check: `http://localhost:5080/health`
- Sample requests: `src/DotnetApiTemplate.Api/DotnetApiTemplate.Api.http`

On Windows you can run `./scripts/setup-local.ps1` instead of step 1.

## Run everything in Docker

```bash
cd docker
cp .env.example .env
docker compose up --build
```

The API is available at `http://localhost:18080` (`API_PORT` in `docker/.env`, picked per project). This builds the production image, so code changes
need `docker compose up --build`.

## Run everything in Docker with auto reload

```bash
cd docker
cp .env.example .env
docker compose -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.dev.yml up --build
```

The API runs `dotnet watch` inside an SDK container with your source mounted, at `http://localhost:18080`.
Save a file under `src/` and the API reloads, with no image rebuild. Changes that can't be hot reloaded
restart the app automatically. The container keeps its own `bin/` and `obj/`, so it doesn't clash with
builds on your machine.

Only rebuild (`--build`) after changing `docker/Dockerfile.dev`. Stop with the same `-f` files and
`down` instead of `up --build`; add `-v` to also delete the database and NuGet cache volumes.

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

Never commit secrets. For local overrides use user secrets:

```bash
dotnet user-secrets --project src/DotnetApiTemplate.Api set "Jwt:SigningKey" "<at least 32 characters>"
```

In production, set configuration through environment variables (`Jwt__SigningKey`, `ConnectionStrings__Default`).
