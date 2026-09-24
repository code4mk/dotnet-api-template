# Changelog

All notable changes to the template are recorded here.
Versions follow [Semantic Versioning](https://semver.org/): breaking structure changes bump the major version.

## [Unreleased]

### Added

- Auto-reload development: `dotnet watch` locally, and `docker/Dockerfile.dev` + `docker-compose.dev.yml`
  to run `dotnet watch` in Docker against the mounted source.
- Per-project host ports generated at `dotnet new` time: `DB_PORT` (54300-54999) for PostgreSQL and
  `API_PORT` (18000-18999) for the API container, so projects don't clash on 5432/8080.

### Changed

- Template short name is now `dotnet-api-template` (was `code4mk-api`).
- ASP.NET Core and EF Core packages and the `dotnet-ef` tool updated to 10.0.12, Npgsql to 10.0.3.
- Central transitive pinning enabled; EF Core Relational pinned to 10.0.12.
- Repository workflows (`template-ci`, `publish`) run manually only.

### Security

- `Microsoft.OpenApi` moves from 2.0.0 to 2.12.0, fixing GHSA-v5pm-xwqc-g5wc.

## [1.0.0] - 2026-09-24

### Added

- Minimal API solution with feature folders and a service layer.
- Sample features: Auth (JWT login), Users, Products.
- `Result<T>` pattern, ProblemDetails errors, .NET 10 built-in validation.
- EF Core with PostgreSQL, development seed data.
- Correlation id middleware, health checks, OpenAPI.
- Docker and Docker Compose, GitHub Actions for build/test and image publishing.
- Unit tests (EF Core in-memory) and integration tests (`WebApplicationFactory`).
- ADRs, architecture overview, API conventions and developer guides.
