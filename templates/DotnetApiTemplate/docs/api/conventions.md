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

How errors are produced in code (catalogs, `Result`, exceptions): [Errors and exceptions](../development/errors-and-exceptions.md).

## JSON

- camelCase names, null properties omitted, enums as strings, dates in ISO 8601 UTC with `Z`.
- Requests are strict: numbers as strings, enums as numbers, duplicate properties, comments and trailing
  commas are rejected with `400 invalid_json` and the location (`at '$.price'`).

Details: [JSON serialization](../development/json-serialization.md).

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

Send the token from `POST /api/auth/login` as `Authorization: Bearer <token>`. Every `/api` endpoint
requires it unless it's marked public. Details: [Authentication](../development/authentication-and-authorization.md).

## CORS

Browser frontends on other origins must be listed in `CORS_ALLOWED_ORIGINS`. Details: [CORS](../development/cors.md).

## Versioning

Not enabled by default. When a project needs it, add `Asp.Versioning.Http` and use URL segments (`/api/v1/...`).
