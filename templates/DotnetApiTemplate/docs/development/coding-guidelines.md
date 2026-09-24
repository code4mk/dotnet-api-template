# Coding guidelines

## Adding a feature

1. Create `src/DotnetApiTemplate.Api/Features/<Feature>/` with five files:
   `<Feature>Endpoints.cs`, `I<Feature>Service.cs`, `<Feature>Service.cs`, `<Feature>Dtos.cs`, `<Feature>Mappings.cs`.
2. Add the entity to `Domain/Entities/` and a configuration to `Data/Configurations/`.
3. Add a `DbSet` to `AppDbContext` and create a migration.
4. Register the service in `AddFeatures()` (`Common/Extensions/ServiceCollectionExtensions.cs`).
5. Map the endpoints in `MapFeatures()` (`Common/Extensions/EndpointExtensions.cs`).
6. Add unit tests for the service and integration tests for the endpoints.

Copy the `Products` feature as a starting point.

## Endpoints

- Named `private static` handler methods, never inline lambdas.
- Return `TypedResults` / `Results<...>`; call exactly one service method.
- Name every endpoint (`WithName`) and add a summary (`WithSummary`).
- Public endpoints must explicitly call `AllowAnonymous()`.

## Services

- All business rules live here.
- Return `Result` / `Result<T>` for expected failures; throw only for truly unexpected problems.
- Keep feature errors in a static `<Feature>Errors` class with stable codes (`feature.error_name`).
- No `HttpContext`, no `IResult`, no ASP.NET types.
- Use `AsNoTracking()` and project with `Select(<Feature>Mappings.ToResponseExpression)` for reads.
- Always pass `CancellationToken` through.

## DTOs

- `sealed record` types; request and response types are separate.
- Validation attributes on request records for input rules.
- Never return entities from endpoints.

## General

- File-scoped namespaces, one public type per file (small related records may share a DTO file).
- Async methods end with `Async` (endpoint handlers are exempt).
- Configuration through strongly typed options classes with `ValidateOnStart`.
- Never commit secrets.

## Pull request checklist

- [ ] Endpoints are thin and return `TypedResults`.
- [ ] Business logic is in the service and covered by unit tests.
- [ ] New endpoints have integration tests, including auth and validation cases.
- [ ] Public endpoints are intentional (`AllowAnonymous`).
- [ ] Migration added if the model changed.
- [ ] No secrets in code or `appsettings.json`.
