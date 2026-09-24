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

# 2. Run the API
dotnet run --project src/DotnetApiTemplate.Api
```

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

The API is available at `http://localhost:8080`.

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
