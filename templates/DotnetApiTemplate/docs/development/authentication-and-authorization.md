# Authentication and authorization

The API uses **JWT bearer tokens**: a client logs in once, then sends the token in the
`Authorization` header. There are no cookies or sessions.

## The flow

```text
POST /api/users        {"fullName","email","password"}   → 201 (register, public)
POST /api/auth/login   {"email","password"}              → 200 {"accessToken","tokenType":"Bearer","expiresAt"}
GET  /api/users        Authorization: Bearer <accessToken> → 200
```

- Passwords are hashed with ASP.NET Core Identity's `PasswordHasher` (PBKDF2); old hashes are upgraded on login.
- Wrong email and wrong password return the same `401 auth.invalid_credentials`, so emails can't be discovered.
- Inactive users (`IsActive = false`) can't log in.
- Tokens expire after `JWT_EXPIRY_MINUTES` (default 60). There are no refresh tokens: the client logs in again.

## Configuration

| Variable | Default | Notes |
| --- | --- | --- |
| `JWT_SIGNING_KEY` | — (required) | HMAC-SHA256 key, at least 32 characters: `openssl rand -base64 48`. Changing it invalidates all tokens. |
| `JWT_ISSUER` / `JWT_AUDIENCE` | `DotnetApiTemplate` | Must match between the service that issues and the one that validates tokens |
| `JWT_EXPIRY_MINUTES` | `60` | 1–1440 |

Code: `Infrastructure/Authentication/` (`AuthenticationExtensions` for validation, `JwtTokenService`
for issuing, `JwtSettings`, `Policies`).

## What's in a token

| Claim | Constant | Value |
| --- | --- | --- |
| `sub` | `AppClaimTypes.UserId` | user id |
| `email` | `AppClaimTypes.Email` | email |
| `name` | `AppClaimTypes.Name` | full name |
| `role` | `AppClaimTypes.Role` | `User` or `Admin` |

Claims keep these short names (inbound claim mapping is off), so read them with the constants.

## Protecting endpoints

**Everything under `/api` requires a valid token by default** (`MapFeatures` calls
`RequireAuthorization()` on the group). You opt out, never in:

```csharp
group.MapGet("/", GetAll)
    .AllowAnonymous();                          // public: anyone can call it

group.MapPut("/{id:int}", Update);              // any logged-in user (the default)

group.MapDelete("/{id:int}", Delete)
    .RequireAuthorization(Policies.Admin);      // only admins
```

A whole group can be public: `api.MapGroup("/auth").AllowAnonymous()`.

Responses: no or invalid token → `401`; valid token but the policy fails → `403`.

## Roles and policies

Roles come from `UserRole` (`User`, `Admin`) and are written into the token at login. The `Admin`
policy requires the `Admin` role. Always use policy constants, never string literals.

**Add a policy:**

```csharp
// Infrastructure/Authentication/Policies.cs
public static class Policies
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
}

// Infrastructure/Authentication/AuthenticationExtensions.cs
services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Admin, policy => policy.RequireRole(nameof(UserRole.Admin)))
    .AddPolicy(Policies.Staff, policy => policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Staff)));
```

A new role also needs a `UserRole` value; the `Role` column is a string (max 20), so no migration is
needed for the enum itself.

**Make a user admin:** there is no admin endpoint or seed user. Update the database directly:

```sql
UPDATE "Users" SET "Role" = 'Admin' WHERE "Email" = 'you@example.com';
```

The user must log in again: roles are read into the token at login.

## The current user in code

Endpoints get the user as a `ClaimsPrincipal` parameter:

```csharp
using System.Security.Claims;

private static async Task<Results<Ok<OrderResponse>, ProblemHttpResult>> Create(
    CreateOrderRequest request,
    ClaimsPrincipal user,
    IOrderService service,
    CancellationToken cancellationToken)
{
    var userId = int.Parse(user.FindFirstValue(AppClaimTypes.UserId)!, CultureInfo.InvariantCulture);
    var result = await service.CreateAsync(userId, request, cancellationToken);
    ...
}
```

Pass the id (not the `ClaimsPrincipal`) into the service; services don't use ASP.NET types.

**Ownership checks** ("users can only edit their own orders") belong in the service:

```csharp
if (order.UserId != userId)
{
    return OrderErrors.NotOwner;     // Error.Forbidden("orders.not_owner", "...")
}
```

## Trying it

- **Swagger UI** (`/swagger`, Development): call `POST /api/auth/login`, copy `accessToken`, click
  **Authorize**, paste it. Endpoints with a lock then send it. See [API documentation](api-documentation.md).
- **`.http` file**: `src/DotnetApiTemplate.Api/DotnetApiTemplate.Api.http` registers, logs in, and reuses the
  token (`{{login.response.body.accessToken}}`).
- **curl:**

  ```bash
  TOKEN=$(curl -s -X POST localhost:5080/api/auth/login -H 'Content-Type: application/json' \
    -d '{"email":"jane@example.com","password":"Password@123"}' | jq -r .accessToken)
  curl localhost:5080/api/users -H "Authorization: Bearer $TOKEN"
  ```

## Testing

Integration tests replace JWT with a test scheme: `factory.CreateAuthenticatedClient("Admin")` sends an
`X-Test-Role` header and is treated as logged in with that role; `factory.CreateClient()` is anonymous.
Test both the allowed and the `401`/`403` cases. See [Testing](testing.md).
