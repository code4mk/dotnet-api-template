# ADR-0002: Feature-Based Folder Structure with a Service Layer

We organize each API as a single project with feature folders, and put business logic in a service layer.

| Field | Value |
| --- | --- |
| Status | Accepted |
| Date | 2026-09-24 |
| Scope | All projects created from the company Minimal API template |

## Context

ADR-0001 chose Minimal APIs but left structure open. We need a layout that is simple for small
projects, scales to enterprise size, and lets developers move between projects without relearning.

## Decision

Every project uses this layout (see the repository root for the full tree):

```text
src/DotnetApiTemplate.Api/
├── Features/<Feature>/     // Endpoints, IService, Service, Dtos, Mappings
├── Domain/                 // entities, enums, base types
├── Data/                   // AppDbContext, configurations, migrations
├── Infrastructure/         // authentication, email, external services
└── Common/                 // errors, middleware, results, pagination, settings, JSON, CORS, OpenAPI, extensions
```

Rules:

1. **Five files per feature:** `XEndpoints`, `IXService`, `XService`, `XDtos`, `XMappings`.
2. **Endpoints are thin.** They call one service method and convert `Result<T>` to `TypedResults`.
3. **Services own business logic.** They never use `HttpContext` or return `IResult`.
4. **Expected failures are results by default.** Services return `Result`/`Result<T>` with an `Error` from the
   feature's catalog; code that can't return one throws the same error (`Error.ToException()`).
5. **Validation on DTOs.** Request records carry data annotations; rules needing the database live in services.
6. **Mappings, not serializers.** Entity-to-DTO conversion lives in `XMappings`; JSON settings are global.
7. **Only services and `Data/` touch `AppDbContext`.**
8. **Features talk through interfaces.** A feature may inject another feature's `IXService`, never its internals.
9. **Split large services** (over roughly 400 lines) inside the same feature folder.

## Consequences

- Code for one feature is in one place; merge conflicts are rare.
- The layout maps directly to separate Api / Application / Domain / Infrastructure projects if a
  system outgrows a single project.
- Discipline is needed to keep services small and endpoints thin; code review enforces it.
