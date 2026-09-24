# Changelog

All notable changes to the template are recorded here.
Versions follow [Semantic Versioning](https://semver.org/): breaking structure changes bump the major version.

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
