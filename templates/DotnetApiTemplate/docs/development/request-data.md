# Request data: route, query string, headers, body and files

How clients send data to the API, and how endpoints receive it.

| Data | Where | Used for |
| --- | --- | --- |
| Which resource | **route** `/api/products/42` | ids, slugs |
| Filters, search, paging, sorting | **query string** `?search=pen&page=2` | `GET` (and `DELETE` options) |
| Metadata | **headers** `Authorization`, `X-Correlation-Id`, `X-Tenant-Id` | auth, tracing, tenancy |
| The data to create or change | **JSON body** `{"name":"Pen","price":1.5}` | `POST`, `PUT`, `PATCH` |
| Files | **multipart/form-data** | uploads only, on their own endpoints |

Rules of thumb: `GET` never has a body; bodies are JSON (`Content-Type: application/json`); files are the only
multipart requests.

## How an endpoint parameter gets its value

Minimal APIs look at each parameter of the handler method and fill it from one source:

| Parameter | Filled from | Example |
| --- | --- | --- |
| Name matches a route segment | route | `int id` for `/{id:int}` |
| Simple type (`int`, `string`, `bool`, `Guid`, `decimal`, `DateTime`, `DateOnly`, enum, arrays of these) | query string | `int? page` ← `?page=2` |
| `[AsParameters] SomeQuery query` | each property from the query (or route) | `ProductQuery` below |
| `[FromHeader(Name = "X-Tenant-Id")] string tenant` | a header | |
| Complex type (a record/class) | the JSON body | `CreateProductRequest request` |
| `IFormFile`, `IFormFileCollection`, `[FromForm] T` | multipart form | uploads |
| Registered service | dependency injection | `IProductService service` |
| `CancellationToken`, `ClaimsPrincipal`, `HttpContext`, `HttpRequest` | the request itself | |

Only **one** parameter can come from the body. Make the source explicit with `[FromRoute]`, `[FromQuery]`,
`[FromHeader]`, `[FromBody]` or `[FromServices]` when it could be ambiguous.

## Route values

```csharp
group.MapGet("/{id:int}", GetById);              // /api/products/42

private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> GetById(
    int id, IProductService service, CancellationToken cancellationToken) { ... }
```

- Always constrain route values: `{id:int}`, `{id:guid}`, `{slug:minlength(3)}`. A value that doesn't match
  (`/api/products/abc`) gives `404`, not a binding error.
- Routes identify a resource; put options in the query string, not in extra route segments.

## Query strings

### Simple parameters

```csharp
group.MapGet("/", GetAll);

private static async Task<Ok<PagedResponse<ProductResponse>>> GetAll(
    IProductService service,
    string? search,          // ?search=pen
    int? page,               // ?page=2
    int? pageSize,           // ?pageSize=20
    CancellationToken cancellationToken)
```

```bash
curl "http://localhost:5080/api/products?search=key%20board&page=2&pageSize=20"
```

- **Make query parameters optional** (`int?`, `string?`) and apply defaults in code or the service; a required
  non-nullable parameter that's missing is a `400`.
- **Names are case-insensitive**: `?pageSize=20` and `?PAGESIZE=20` both work. Document and use camelCase.
- **URL-encode values** (`key%20board`, `%2B` for `+`). Browsers and HTTP clients do it for you when you build
  the query with their APIs (`URLSearchParams`, `HttpUtility`).

### Value formats

| Type | Send | Notes |
| --- | --- | --- |
| `int`, `long`, `decimal`, `double` | `?page=2`, `?price=49.99` | invariant culture: `.` as decimal separator |
| `bool` | `?active=true` / `false` | |
| `Guid` | `?key=3f2504e0-4f89-11d3-9a0c-0305e82c3301` | |
| **enum** | `?role=Admin` | **exact name, case-sensitive** in the query (`admin` is a `400`); JSON bodies accept any case |
| `DateTime` | `?from=2026-09-25T10:00:00Z` or `...T12:00:00+02:00` | **always send `Z` or an offset**: offsets are converted to UTC; a value without one is taken as-is |
| `DateOnly` | `?day=2026-09-25` | |
| arrays | `?ids=1&ids=2&ids=3` | **repeat the key**; `?ids=1,2,3` is a `400`; missing means an empty array |

An invalid value (`?page=abc`, `?role=superuser`) returns:

```json
{ "title": "bad_request", "status": 400, "detail": "A parameter has an invalid value.", "correlationId": "..." }
```

In Development the response also names the parameter (`exception.message`: `Failed to bind parameter "int? page" from "abc"`).

### Many filters: `[AsParameters]`

When an endpoint has more than three or four query parameters, group them in a record:

```csharp
// Features/Products/ProductDtos.cs
public sealed record ProductQuery(
    [StringLength(100)] string? Search,
    [Range(0, 1_000_000)] decimal? MinPrice,
    [Range(0, 1_000_000)] decimal? MaxPrice,
    bool? InStock,
    ProductSort? Sort,               // enum: ?sort=PriceDesc
    int[]? CategoryIds,              // ?categoryIds=1&categoryIds=4
    [Range(1, int.MaxValue)] int? Page,
    [Range(1, 100)] int? PageSize);

// endpoint
private static async Task<Ok<PagedResponse<ProductResponse>>> GetAll(
    [AsParameters] ProductQuery query, IProductService service, CancellationToken cancellationToken) =>
    TypedResults.Ok(await service.GetAllAsync(query, cancellationToken));
```

```bash
curl "http://localhost:5080/api/products?search=pen&minPrice=1&inStock=true&sort=PriceDesc&categoryIds=1&categoryIds=4&page=1"
```

- Every property is a query parameter with the same name (case-insensitive).
- **Validation attributes run**, like on bodies: `?page=0&pageSize=500` returns `400` with
  `"errors": { "Page": [...], "PageSize": [...] }`.
- Swagger shows each property as a separate query parameter.
- Pass the whole record to the service; it's a plain data object with no ASP.NET types.

### Paging, sorting and searching conventions

- Paging: `page` (from 1) and `pageSize` (max 100); list responses are `PagedResponse<T>`
  (see [API conventions](../api/conventions.md#pagination)).
- Sorting: an enum (`ProductSort.Name`, `PriceAsc`, `PriceDesc`), not a free-text column name; map it to
  `OrderBy` in the service. Never pass raw strings into dynamic ordering or SQL.
- Searching: one `search` parameter for free text; specific filters get their own typed parameters.
- Keep URLs well below ~2,000 characters. A search with very large input belongs in a
  `POST /api/products/search` with a JSON body.

## Headers

```csharp
private static async Task<Ok<ReportResponse>> GetReport(
    [FromHeader(Name = "X-Tenant-Id")] string? tenantId, ...)
```

- Use headers for metadata about the request (tenant, idempotency key, client version), not for business data.
- Don't read `Authorization` yourself: authentication has already validated it; take a `ClaimsPrincipal user`
  parameter (see [Authentication](authentication-and-authorization.md)).
- `X-Correlation-Id` is handled for you (see [Logging](logging-and-correlation-ids.md)).
- A new custom header that browsers send must be allowed in CORS (`WithHeaders` in `CorsExtensions`).

## JSON bodies (`POST`, `PUT`, `PATCH`)

```csharp
// Features/Products/ProductDtos.cs
public sealed record CreateProductRequest(
    [Required, StringLength(200)] string Name,
    [StringLength(2000)] string? Description,
    [Range(typeof(decimal), "0.01", "1000000")] decimal Price,
    [Range(0, int.MaxValue)] int Stock);

// endpoint: the record parameter comes from the body
private static async Task<Results<CreatedAtRoute<ProductResponse>, ProblemHttpResult>> Create(
    CreateProductRequest request, IProductService service, CancellationToken cancellationToken)
```

```bash
curl -X POST http://localhost:5080/api/products \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Pen","description":"Blue","price":1.5,"stock":100}'
```

```ts
await fetch("/api/products", {
  method: "POST",
  headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
  body: JSON.stringify({ name: "Pen", description: "Blue", price: 1.5, stock: 100 }),
});
```

- **`Content-Type: application/json` is required.** `form-data`, `x-www-form-urlencoded` or `text/plain` to a JSON
  endpoint returns `415 Unsupported Media Type`. In Postman: **Body → raw → JSON**.
- Property names are case-insensitive; send camelCase.
- **Validation** (data annotations on the record) runs before the handler: invalid input returns `400` with
  `errors` per field. Rules that need the database (unique name) go in the service.
- **Strict JSON:** numbers as numbers (`1.5`, not `"1.5"`), enums by name, ISO 8601 dates, no duplicate
  properties. Violations return `400 invalid_json` with the location (`at '$.price'`). Details:
  [JSON serialization](json-serialization.md).
- A missing or empty body returns `400 bad_request` "A JSON request body is required."
- Unknown properties are ignored.
- One record per operation: `CreateXRequest`, `UpdateXRequest`. Never bind entities directly (clients could set
  fields like `Role` or `Id`).

### `PUT`: full update

`PUT` replaces the editable fields: the client sends all of them (the template's `UpdateProductRequest` and
`UpdateUserRequest` work this way). Simple and unambiguous; use it by default.

### `PATCH`: partial update

Choose one approach per API:

**A. Optional fields** (simplest): a record where every property is nullable, and `null` means "don't change":

```csharp
public sealed record PatchProductRequest([StringLength(200)] string? Name, decimal? Price, int? Stock);

// service
if (request.Name is not null) product.Name = request.Name.Trim();
if (request.Price is not null) product.Price = request.Price.Value;
```

Limitation: a client can't set a field **to** `null` this way (`null` and "not sent" look the same).

**B. JSON Patch** (RFC 6902): precise operations, including removing values. Add the package
`Microsoft.AspNetCore.JsonPatch.SystemTextJson` (see [Packages](packages-and-dependencies.md)):

```csharp
group.MapPatch("/{id:int}", Patch).WithName("PatchProduct").WithSummary("Partially update a product (JSON Patch).");

private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> Patch(
    int id, JsonPatchDocument<UpdateProductRequest> patch, IProductService service, CancellationToken cancellationToken)
{
    // service: load the product, map it to UpdateProductRequest, patch.ApplyTo(dto), validate, save
}
```

```bash
curl -X PATCH http://localhost:5080/api/products/42 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json-patch+json" \
  -d '[{"op":"replace","path":"/price","value":9.5},{"op":"remove","path":"/description"}]'
```

The content type **must** be `application/json-patch+json` (plain `application/json` is a `415`). Apply the patch
to a request DTO, never to the entity, and validate the result before saving.

## Files: `multipart/form-data`

Files are uploaded to their own endpoints; everything else stays JSON. Typical pattern:

```text
1. POST /api/users/42/avatar   multipart/form-data  (file=@photo.jpg)   → 200 {"url": "..."}
2. PUT  /api/users/42          application/json     (other fields)       unchanged
```

For "create with a file", upload first and send the returned id/URL in the JSON body.

### One file

```csharp
group.MapPost("/{id:int}/avatar", UploadAvatar)
    .WithName("UploadUserAvatar")
    .WithSummary("Upload the user's picture (JPEG/PNG/WebP, max 2 MB).")
    .DisableAntiforgery()                                  // required, see below
    .WithMetadata(new RequestSizeLimitAttribute(3_000_000));   // reject bigger requests early

private static async Task<Results<Ok<AvatarResponse>, ProblemHttpResult>> UploadAvatar(
    int id, IFormFile file, IUserService service, CancellationToken cancellationToken)
{
    var result = await service.SetAvatarAsync(id, file, cancellationToken);
    return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
}
```

```bash
curl -X POST http://localhost:5080/api/users/42/avatar \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@photo.jpg;type=image/jpeg"
```

```ts
const form = new FormData();
form.append("file", input.files[0]);            // the field name must match the parameter name
await fetch("/api/users/42/avatar", { method: "POST", headers: { Authorization: `Bearer ${token}` }, body: form });
// don't set Content-Type: the browser adds multipart/form-data with the boundary
```

In Postman: **Body → form-data**, key `file`, type **File**. Swagger UI shows a file picker.

### Several files

```csharp
private static async Task<Ok<UploadResponse>> UploadAttachments(IFormFileCollection files, ...)   // -F "files=@a.pdf" -F "files=@b.pdf"
```

### Fields and a file together

```csharp
public sealed record DocumentUpload(string Title, string? Category, IFormFile File);

group.MapPost("/", Upload).DisableAntiforgery();

private static async Task<...> Upload([FromForm] DocumentUpload form, IDocumentService service, CancellationToken ct)
```

```bash
curl -X POST http://localhost:5080/api/documents -H "Authorization: Bearer $TOKEN" \
  -F "title=Q3 report" -F "category=finance" -F "file=@report.pdf"
```

A missing form field makes binding fail with a generic `400 bad_request` rather than per-field `errors`, so keep
form records small and validate in the service (return `Error.Validation(...)` or
`throw RequestValidationException.ForField(...)`).

### Why `.DisableAntiforgery()`

.NET protects every endpoint that reads form data with antiforgery tokens, against CSRF from other websites.
Without `.DisableAntiforgery()`, requests to the endpoint fail with `500` ("contains anti-forgery metadata, but a
middleware was not found"). Disabling it is safe **here** because the API authenticates with a bearer token in the
`Authorization` header, which another site's form can't send. If a project ever uses **cookie** authentication,
enable antiforgery instead (`AddAntiforgery` + `UseAntiforgery`).

### Upload rules

- **Limit the size**: check `file.Length` in the service and set `RequestSizeLimit` on the endpoint (the global
  default is about 30 MB).
- **Don't trust the client**: `file.ContentType` and `file.FileName` are whatever the client sent. Allow-list
  content types, check the first bytes for security-sensitive formats, and **never use the uploaded file name
  as a path**: generate the storage key (`{Guid}.jpg`).
- **Store files outside the app** (S3-compatible storage in production, a mounted volume or local folder in
  development), not in the container's filesystem, which is lost on redeploy and differs per instance.
- **Stream, don't buffer**: `await using var stream = file.OpenReadStream();` and pass the stream to storage.
- **Large files** (videos, big documents): return a pre-signed upload URL from the API and let the client upload
  directly to storage.
- **Images**: re-encode or resize them before storing (strips metadata and hidden payloads).
- Base64 inside JSON only for tiny files (it's 33% larger and fully buffered).

## Form-urlencoded

Normal endpoints don't accept `application/x-www-form-urlencoded` (`415`). If one endpoint must (a webhook or
OAuth-style callback from a third party), bind it explicitly:

```csharp
public sealed record WebhookForm(string Event, string Signature);

group.MapPost("/webhooks/payments", HandleWebhook).AllowAnonymous().DisableAntiforgery();

private static async Task<Ok> HandleWebhook([FromForm] WebhookForm form, IPaymentService service, CancellationToken ct)
```

Verify such requests another way (a signature header or shared secret), since they don't carry a bearer token.

## Other handler parameters

| Parameter | Gives |
| --- | --- |
| `IProductService service` (any registered service) | dependency injection |
| `CancellationToken cancellationToken` | cancelled when the client disconnects: always pass it on |
| `ClaimsPrincipal user` | the authenticated user ([Authentication](authentication-and-authorization.md)) |
| `HttpContext` / `HttpRequest` | raw access; avoid: pass plain values to services instead |

Services never receive `HttpContext`, `IFormFile` streams stay in the endpoint/service boundary, and the handler
passes plain values (ids, DTOs, streams) on.

## OpenAPI

The document is generated from the handler signature: route and query parameters, `[AsParameters]` properties,
headers, the body record and `IFormFile` all appear in Swagger with their types. Add what the signature can't
express:

```csharp
.Accepts<CreateProductRequest>("application/json")            // explicit body content type
.Accepts<JsonPatchDocument<UpdateProductRequest>>("application/json-patch+json")
.ProducesValidationProblem()                                   // documents 400 with errors
.ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
```

See [API documentation](api-documentation.md).

## Errors at a glance

| Situation | Status | `title` |
| --- | --- | --- |
| Route value doesn't match the constraint (`/products/abc`) | 404 | `Not Found` |
| Query/route/header value can't be converted (`?page=abc`, `?role=admin`) | 400 | `bad_request` |
| Required query parameter missing | 400 | `bad_request` |
| `[AsParameters]` or body fails validation | 400 | "One or more validation errors occurred." + `errors` |
| Body missing | 400 | `bad_request` ("A JSON request body is required.") |
| Body isn't valid JSON / wrong types | 400 | `invalid_json` (with `$.path`) |
| Wrong `Content-Type` (form data to a JSON endpoint, JSON to an upload) | 415 | `Unsupported Media Type` |
| Upload endpoint without `.DisableAntiforgery()` | 500 | `server_error` (fix the endpoint) |
| Request bigger than the size limit | 413 | `payload_too_large` |

## Quick reference

```text
GET    /api/products?search=pen&page=2&pageSize=20&ids=1&ids=2    query string (repeat keys for arrays)
GET    /api/products/42                                            route
POST   /api/products          Content-Type: application/json       { ...create DTO... }
PUT    /api/products/42       Content-Type: application/json       { ...all editable fields... }
PATCH  /api/products/42       Content-Type: application/json       { ...nullable fields... }            (approach A)
PATCH  /api/products/42       Content-Type: application/json-patch+json  [ {"op": ...} ]          (approach B)
DELETE /api/products/42
POST   /api/users/42/avatar   multipart/form-data                  file=@photo.jpg                     (uploads only)
```
