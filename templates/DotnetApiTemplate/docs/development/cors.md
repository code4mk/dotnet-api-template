# CORS

CORS (cross-origin resource sharing) decides which **browser** pages on other origins may call the API.
Browsers enforce it; everything else ignores it.

| Client | Needs CORS? |
| --- | --- |
| Frontend on another origin: `http://localhost:5173` (Vite), `https://app.example.com` → `https://api.example.com` | yes |
| Frontend and API on the same origin (reverse proxy: `example.com/` and `example.com/api`) | no |
| Mobile apps, other backends, Postman, curl | no |
| Swagger UI (`/swagger`, served by the API itself) | no |

An *origin* is scheme + host + port: `http://localhost:5173` and `http://localhost:3000` are different
origins, and so are `http://` and `https://`.

## Configure

One variable in `.env`, comma-separated exact origins:

```bash
CORS_ALLOWED_ORIGINS=http://localhost:5173,http://localhost:3000   # development (default in .env.example)
CORS_ALLOWED_ORIGINS=https://app.example.com,https://admin.example.com   # production
CORS_ALLOWED_ORIGINS=                                               # none: no cross-origin browser access
```

Spaces and trailing slashes are ignored. Invalid values stop the app at startup with a message:

| Value | Result |
| --- | --- |
| `*`, `https://*.example.com` | rejected: wildcards are not allowed, list each origin |
| `app.example.com` | rejected: expected `scheme://host[:port]` |
| `https://app.example.com/login` | rejected: an origin has no path |
| `ftp://example.com` | rejected: only http and https |

## What the policy allows

`Common/Cors/CorsExtensions.cs`:

| | Value |
| --- | --- |
| Origins | exactly the listed ones |
| Methods | `GET`, `POST`, `PUT`, `PATCH`, `DELETE` |
| Request headers | `Authorization`, `Content-Type`, `Accept`, `X-Correlation-Id` |
| Headers readable by frontend code | `X-Correlation-Id`, `Location` |
| Credentials (cookies) | not allowed: send the JWT in the `Authorization` header |
| Preflight cache | 10 minutes |

CORS runs before error handling and authentication, so preflight (`OPTIONS`) requests need no token,
and error responses carry CORS headers too: the frontend can read the ProblemDetails body of a `400`
or `401`.

## Calling the API from a frontend

```ts
const response = await fetch("http://localhost:5080/api/products", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  },
  body: JSON.stringify({ name: "Keyboard", price: 49.99, stock: 5 }),
});

if (!response.ok) {
  const problem = await response.json();          // ProblemDetails: title, detail, errors
  const correlationId = response.headers.get("X-Correlation-Id");   // readable thanks to the exposed header
}
```

Don't use `credentials: "include"`: the API doesn't use cookies, and the policy doesn't allow them.

## Changing the policy

- **Another header** (e.g. `X-Tenant-Id`): add it to `WithHeaders(...)`.
- **Another readable response header**: add it to `WithExposedHeaders(...)`.
- **Cookies** (only if you switch to cookie auth): add `.AllowCredentials()`. This is only safe with exact
  origins, which the validation already enforces, and you then also need CSRF protection.
- **Different rules per endpoint**: add named policies with `cors.AddPolicy("name", ...)` and use
  `.RequireCors("name")` on the endpoint or group.

## Troubleshooting

| Browser console says | Fix |
| --- | --- |
| `No 'Access-Control-Allow-Origin' header is present` | Add the exact origin (check the port and http vs https) to `CORS_ALLOWED_ORIGINS` and restart |
| `Request header field x-foo is not allowed` | Add the header to `WithHeaders(...)` |
| `The value of the 'Access-Control-Allow-Credentials' header ... must be 'true'` | Remove `credentials: "include"` from the frontend request |
| Works in Postman, fails in the browser | Expected: Postman doesn't enforce CORS. Fix the origin list |

Check the headers yourself:

```bash
curl -i -X OPTIONS http://localhost:5080/api/products \
  -H "Origin: http://localhost:5173" -H "Access-Control-Request-Method: POST"
```

An allowed origin gets `Access-Control-Allow-Origin: http://localhost:5173`; others get no CORS headers.
