# API documentation (Swagger)

.NET generates an **OpenAPI document** from the endpoint code, and **Swagger UI** renders it as an
interactive page. Both are served in Development only.

| URL | What |
| --- | --- |
| `http://localhost:5080/swagger` | Swagger UI: browse and call every endpoint |
| `http://localhost:5080/openapi/v1.json` | The OpenAPI document (import into Postman, generate clients) |

In Staging and Production both return `404`. To enable them on Staging, change the check in
`Program.cs` from `IsDevelopment()` to `!app.Environment.IsProduction()`.

## Calling protected endpoints

1. Register (`POST /api/users`) and log in (`POST /api/auth/login`) with **Try it out** → **Execute**.
2. Copy `accessToken` from the response.
3. Click **Authorize** (top right), paste the token (without `Bearer `), **Authorize**.
4. Endpoints with a lock now send `Authorization: Bearer <token>`. The token is kept across page reloads.

Only endpoints that really need a token show the lock: `BearerSecurityTransformer` marks operations
that require authorization and aren't `AllowAnonymous`.

## How the document is built

```text
endpoint code (MapGet/MapPost, WithSummary, types)
  → built-in OpenAPI generator (AddOpenApi)          Common/OpenApi/OpenApiExtensions.cs
  → transformers: title/version/description, Bearer security, DateTime schema fix
  → /openapi/v1.json
  → Swagger UI (Swashbuckle.AspNetCore.SwaggerUI, UI only) reads it
```

You never write OpenAPI JSON by hand: improve the endpoint code and the document follows.

## Documenting endpoints well

```csharp
group.MapGet("/{id:int}", GetById)
    .WithName("GetProductById")                       // operationId: stable name for generated clients
    .WithSummary("Get one product.")                  // one line shown in the list
    .WithDescription("Returns 404 if the product doesn't exist.")   // longer text (optional)
    .AllowAnonymous();

group.MapPost("/", Create)
    .WithName("CreateProduct")
    .WithSummary("Create a product.")
    .ProducesValidationProblem();                     // documents the 400 with field errors
```

| Where it comes from | Shown as |
| --- | --- |
| `WithTags("Products")` on the group | section heading |
| Request/response record types | schemas, with `[Required]`, `[StringLength]`, `[Range]` as constraints |
| Typed returns: `Results<Ok<ProductResponse>, NotFound, ProblemHttpResult>` | the documented status codes |
| `.Produces<T>(201)`, `.ProducesProblem(404)` | extra status codes when the return type can't express them |
| XML doc comments (`/// <summary>`) on DTO properties | not included by default; add `<GenerateDocumentationFile>` if you want them |

Hide an endpoint from the docs with `.ExcludeFromDescription()`.

## Using the document elsewhere

- **Postman / Insomnia:** import `http://localhost:5080/openapi/v1.json`.
- **Generated clients:** [Kiota](https://learn.microsoft.com/openapi/kiota/),
  [NSwag](https://github.com/RicoSuter/NSwag) or `openapi-typescript` for a typed frontend client.
  Stable `WithName` values keep generated method names stable.
- **In CI:** you can export the document at build time (`Microsoft.Extensions.ApiDescription.Server`) and
  diff it in pull requests to spot breaking changes.

## Customizing

All in `Common/OpenApi/OpenApiExtensions.cs`:

- Title, version, description: the first `AddDocumentTransformer`.
- Swagger UI URL: `options.RoutePrefix` in `MapApiDocs`.
- More transformers: `AddDocumentTransformer`, `AddOperationTransformer`, `AddSchemaTransformer`.

Tests in `tests/.../Common/OpenApiTests.cs` check that Swagger is served in Development only and that
protected operations are marked.
