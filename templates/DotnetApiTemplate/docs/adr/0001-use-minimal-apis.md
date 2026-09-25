# ADR-0001: Use ASP.NET Core Minimal APIs as the Standard for HTTP APIs

We will build all new HTTP APIs, small and enterprise, with ASP.NET Core Minimal APIs.

| Field | Value |
| --- | --- |
| Status | Accepted |
| Date | 2026-09-24 |
| Scope | All new ASP.NET Core HTTP APIs, small projects and enterprise projects |
| Target platform | .NET 10 (LTS) and later |

## Context

We need one API style for every upcoming ASP.NET Core project. Our portfolio mixes small projects
(a few endpoints, one or two developers) and enterprise projects (many modules, larger teams, long life).

ASP.NET Core Minimal APIs define HTTP endpoints with lightweight mapping methods such as `MapGet` and
`MapPost`. They are built into ASP.NET Core, maintained by Microsoft, and receive new features in every
.NET release.

Forces:

- **One standard for all sizes.** The same style must work for a 5-endpoint service and a 300-endpoint system.
- **Maintainability.** Code must stay easy to read and change as projects grow.
- **Onboarding.** New developers should learn from official Microsoft docs and samples.
- **Long-term support.** Enterprise systems live for years, so we need first-party, supported technology.
- **OData.** Some enterprise projects may need OData queries for reporting and BI tools.
- **Performance.** Low framework overhead is welcome, but it is not the deciding factor.

## Decision

We adopt ASP.NET Core Minimal APIs as the standard for all new HTTP APIs.

1. **Minimal APIs only.** All HTTP endpoints are defined with Minimal API mapping methods (`MapGet`, `MapPost`, `MapPut`, `MapDelete`).
2. **Route groups.** Related endpoints are grouped with `MapGroup` in one class per feature that implements `IEndpoints` (for example `OrderEndpoints`), mapped by `MapFeatures()` by hand or by auto-discovery. `Program.cs` only wires features; it holds no endpoint logic.
3. **No logic in lambdas.** Endpoints are named static methods. Business logic sits in services, never inline in `Program.cs`.
4. **TypedResults.** Endpoints return `TypedResults` and `Results<T1, T2>` so responses are type-checked and appear correctly in OpenAPI.
5. **Validation.** Use the built-in Minimal API validation in .NET 10 for simple rules; FluentValidation through an endpoint filter is allowed for complex rules.
6. **Errors.** All errors return RFC 7807 `ProblemDetails` through `AddProblemDetails` and a global exception handler.
7. **OpenAPI.** Every project uses the built-in `Microsoft.AspNetCore.OpenApi` document generation.
8. **Security by group.** Authorization is applied at group level (`RequireAuthorization`) and relaxed per endpoint only with `AllowAnonymous`.

Project and folder structure is decided in [ADR-0002](0002-project-folder-structure.md).

## Consequences

**Positive**

- One API style across small and enterprise projects, so developers move between teams easily.
- First-party technology with long-term Microsoft support.
- Little boilerplate and low framework overhead.
- Official docs, samples and dotnet/eShop serve as learning material.
- OData, OpenAPI, validation and versioning all work with Minimal APIs.

**Negative**

- Code organization is a team convention, not enforced by the framework.
- Developers new to Minimal APIs need a short learning period.
- OData support on Minimal APIs is newer and has fewer examples.

**Risks and mitigations**

| Risk | Mitigation |
| --- | --- |
| Endpoint logic grows inside `Program.cs` | Code review checklist and route groups per area |
| Inconsistent endpoint style across teams | Shared project template and the conventions in this ADR |
| OData needs exceed current Minimal API support | Proof of concept before the first project that needs OData |

## References

- [dotnet/eShop](https://github.com/dotnet/eshop): Microsoft reference app using Minimal APIs with .NET Aspire
- [Enable OData functionalities on ASP.NET Core Minimal API](https://devblogs.microsoft.com/odata/?p=5932): support since Microsoft.AspNetCore.OData 9.4.0
- [OData/AspNetCoreOData](https://github.com/OData/aspnetcoreodata): official OData library, includes the ODataMiniApi sample
- [dotnet/aspnet-api-versioning](https://github.com/dotnet/aspnet-api-versioning): API versioning for Minimal APIs
