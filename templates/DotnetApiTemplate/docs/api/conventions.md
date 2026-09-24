# API conventions

## Routes

- Prefix every route with `/api`.
- Use plural, lowercase nouns: `/api/products`, `/api/products/{id}`.
- Constrain route parameters: `/{id:int}`.
- Use HTTP verbs for actions: `GET` read, `POST` create, `PUT` full update, `DELETE` remove.

## Responses

| Case | Status | Body |
| --- | --- | --- |
| Read one / update | `200 OK` | resource |
| Create | `201 Created` + `Location` header | created resource |
| Delete | `204 No Content` | none |
| Invalid input | `400 Bad Request` | validation `ProblemDetails` with `errors` |
| Missing or malformed body, bad parameter | `400 Bad Request` | `ProblemDetails`, title `bad_request` / `invalid_json` |
| Not authenticated | `401 Unauthorized` | none or `ProblemDetails` |
| Not allowed | `403 Forbidden` | none |
| Not found | `404 Not Found` | `ProblemDetails` |
| Conflict (duplicate, state) | `409 Conflict` | `ProblemDetails` (also for database unique/foreign-key violations) |
| Business rule not met | `422 Unprocessable Entity` | `ProblemDetails` |
| External service failed | `502 Bad Gateway` / `503` | `ProblemDetails` with a safe message |
| Server error | `500` | generic `ProblemDetails`, title `server_error` (no internals) |

Error `title` is a stable machine-readable code such as `products.not_found`; `detail` is human-readable.
Every error response is `application/problem+json` and includes `correlationId`, the same value as the
`X-Correlation-Id` response header: ask clients to send it when they report a problem, and search the
logs for it.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "bad_request",
  "status": 400,
  "detail": "A JSON request body is required.",
  "instance": "POST /api/auth/login",
  "correlationId": "f9f6c18ea0ed4b35b2f8c24c856b0e33"
}
```

### Errors in code

Each feature keeps its errors in one catalog, next to the service:

```csharp
public static class ProductErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("products.not_found", $"Product with id {id} was not found.");

    public static Error HasOpenOrders(int id) =>
        Error.BusinessRule("products.has_open_orders", $"Product {id} has open orders and can't be deleted.");
}
```

There are two ways to use a catalog error, and both give the **same** response:

| Where | How |
| --- | --- |
| Services (preferred) | `return ProductErrors.NotFound(id);`, then `result.ToProblem()` in the endpoint |
| Deep helpers, domain entities, code that can't return a `Result` | `throw ProductErrors.NotFound(id).ToException();` |

Ready-made exceptions (`Common/Errors/Exceptions/`) for cases without a catalog entry:

| Exception | Status | Example |
| --- | --- | --- |
| `RequestValidationException` | 400 | `throw RequestValidationException.ForField("startDate", "Must be before endDate.");` (sends `errors` like built-in validation) |
| `UnauthorizedException` | 401 | `throw new UnauthorizedException();` |
| `ForbiddenException` | 403 | `throw new ForbiddenException("orders.not_owner", "You can only change your own orders.");` |
| `NotFoundException` | 404 | `throw NotFoundException.For("Product", id);` → `product.not_found` |
| `ConflictException` | 409 | `throw new ConflictException("orders.already_paid", "The order is already paid.");` |
| `BusinessRuleException` | 422 | `throw new BusinessRuleException("products.has_open_orders", "...");` |
| `ExternalServiceException` | 502 / 503 | `throw new ExternalServiceException("payments.failed", "The payment provider failed.", ex);` |

Their message is sent to clients, so keep internals out of it; pass the original exception as the inner
exception instead (it is logged, never returned). Thrown 4xx are logged as information, 5xx with the stack trace.

- **Everything else thrown** ends in `Common/Errors/GlobalExceptionHandler` too. `ExceptionMapping` decides the
  status, code, safe message and log level: client-caused errors (bad JSON, missing body, unique
  violations, aborted requests) are 4xx and logged as one-line warnings; anything unknown is a `500`
  logged with its stack trace. Add a case to `ExceptionMapping` when a library throws for what is
  really a client error.
- In Development the response also contains an `exception` object (type and message). Production
  never returns the messages of unexpected exceptions; only the `AppException` messages you write are sent.

## JSON

One configuration for everything (request/response bodies, ProblemDetails, OpenAPI):
`Common/Json/JsonDefaults.cs`. Use `JsonDefaults.Options` for any manual `JsonSerializer` call.

Responses:

- camelCase property names; null properties are omitted.
- Enums as strings (`"role": "Admin"`).
- Dates in ISO 8601 UTC with `Z`: `"createdAt": "2026-09-25T10:15:30.123Z"`.
- Non-ASCII text stays readable (`"José"`, not `"Jos\u00E9"`); `<`, `>`, `&` are escaped.

Requests are read strictly; these fail with `400 invalid_json` and the location (`at '$.price'`):

- Numbers as strings (`"price": "49.99"`), enums as numbers (`"role": 1`), dates that aren't ISO 8601.
- Duplicate properties (`{"name": "A", "name": "B"}`), comments, trailing commas.
- Nesting deeper than 32 levels.

Property names are case-insensitive and unknown properties are ignored. Dates with an offset
(`+02:00`) are converted to UTC; dates without one are read as UTC. Missing or empty required fields
are reported by validation (`400` with `errors` per field), not as `invalid_json`.

## Pagination

List endpoints accept `page` (default 1) and `pageSize` (default 20, max 100) and return:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

## Authentication

Send the token from `POST /api/auth/login` as `Authorization: Bearer <token>`.

## Versioning

Not enabled by default. When a project needs it, add `Asp.Versioning.Http` and use URL segments (`/api/v1/...`).
