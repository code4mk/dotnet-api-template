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

- **Expected failures** (not found, duplicate, not allowed) are returned as `Result` errors from services
  and converted with `ToProblem()`. Don't throw for them.
- **Everything thrown** ends in `Common/Errors/GlobalExceptionHandler`. `ExceptionMapping` decides the
  status, code, safe message and log level: client-caused errors (bad JSON, missing body, unique
  violations, aborted requests) are 4xx and logged as one-line warnings; anything unknown is a `500`
  logged with its stack trace. Add a case to `ExceptionMapping` when a library throws for what is
  really a client error.
- In Development the response also contains an `exception` object (type and message). Production
  never returns exception messages.

## JSON

- camelCase property names.
- Enums as strings (`"role": "Admin"`).
- Null properties are omitted.
- Dates in ISO 8601 UTC.

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
