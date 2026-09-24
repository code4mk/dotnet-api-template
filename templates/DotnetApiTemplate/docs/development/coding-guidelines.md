# Coding guidelines

## Features

Five files per feature: `<Feature>Endpoints.cs`, `I<Feature>Service.cs`, `<Feature>Service.cs`,
`<Feature>Dtos.cs`, `<Feature>Mappings.cs`. Step by step: [Adding a feature](adding-a-feature.md).

## Endpoints

- Named `private static` handler methods, never inline lambdas.
- Return `TypedResults` / `Results<...>`; call exactly one service method.
- Name every endpoint (`WithName`) and add a summary (`WithSummary`).
- Public endpoints must explicitly call `AllowAnonymous()`.

## Services

- All business rules live here.
- Return `Result` / `Result<T>` for expected failures; throw (`AppException`s or `Error.ToException()`) only where
  returning is impractical. See [Errors and exceptions](errors-and-exceptions.md).
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
- Configuration through typed settings classes bound from environment variables (`AddEnvSettings`, see
  [Configuration](configuration-and-environments.md)); `appsettings.json` is only for logging.
- Log with message templates (`"Order {OrderId} placed"`), never interpolated strings; never log secrets.
- Use `TimeProvider` / UTC for time, never `DateTime.Now`.
- Never commit secrets.

## Pull request checklist

- [ ] Endpoints are thin and return `TypedResults`.
- [ ] Business logic is in the service and covered by unit tests.
- [ ] New endpoints have integration tests, including auth and validation cases.
- [ ] Public endpoints are intentional (`AllowAnonymous`).
- [ ] Migration added if the model changed, and its `Up`/`Down` reviewed (no unintended drops).
- [ ] No secrets in code, `appsettings.json` or `.env.example`; new variables added to `.env.example`.
- [ ] New error codes follow `feature.error_name` and error paths are tested.
- [ ] Docs updated if behavior, settings or commands changed.
