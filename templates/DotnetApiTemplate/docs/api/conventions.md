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
| Not authenticated | `401 Unauthorized` | none or `ProblemDetails` |
| Not allowed | `403 Forbidden` | none |
| Not found | `404 Not Found` | `ProblemDetails` |
| Conflict (duplicate, state) | `409 Conflict` | `ProblemDetails` |
| Server error | `500` | generic `ProblemDetails` (no internals) |

Error `title` is a stable machine-readable code such as `products.not_found`; `detail` is human-readable.

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
