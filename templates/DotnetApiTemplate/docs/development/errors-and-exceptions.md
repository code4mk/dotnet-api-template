# Errors and exceptions

Every error response is RFC 7807 `ProblemDetails` (`application/problem+json`), whether it comes from
a returned `Result`, a thrown exception, validation, or a missing route:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "products.not_found",
  "status": 404,
  "detail": "Product with id 42 was not found.",
  "instance": "GET /api/products/42",
  "correlationId": "f9f6c18ea0ed4b35b2f8c24c856b0e33"
}
```

- `title` is a **stable error code** that clients can rely on (`feature.error_name`); `detail` is for humans.
- `correlationId` matches the `X-Correlation-Id` response header and every log line of the request.
- Validation errors add `errors`: `{ "Email": ["The Email field is not a valid e-mail address."] }`.
- In Development only, an `exception` object (type and message) is added for unexpected errors.

## Decision guide

| Situation | Do this | Status |
| --- | --- | --- |
| Input doesn't match the DTO's rules (`[Required]`, `[StringLength]`, ...) | Nothing: built-in validation answers | 400 + `errors` |
| Expected failure in a service (not found, duplicate, wrong credentials) | `return FeatureErrors.X(...)` | from the error type |
| Same, but deep in a helper or entity that can't return a `Result` | `throw FeatureErrors.X(...).ToException()` | same as returning it |
| A rule needing data (e.g. "end date after start date" across fields) | `return Error.Validation(...)` or `throw RequestValidationException.ForField(...)` | 400 |
| A business rule forbids a valid request | `Error.BusinessRule(...)` / `BusinessRuleException` | 422 |
| An external service failed | `throw new ExternalServiceException(..., ex)` | 502 / 503 |
| A bug or an unexpected failure | Don't catch it: let it propagate | 500 (logged with stack trace) |

## 1. Error catalogs

Each feature keeps its errors in one static class next to its service. Codes are `feature.error_name`,
in snake_case, and never change once clients use them.

```csharp
// Features/Products/ProductService.cs (bottom of the file)
public static class ProductErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("products.not_found", $"Product with id {id} was not found.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("products.name_exists", $"A product named '{name}' already exists.");

    // Example of a business rule (not in the sample code):
    public static Error HasOpenOrders(int id) =>
        Error.BusinessRule("products.has_open_orders", $"Product {id} has open orders and can't be deleted.");
}
```

| Factory | `ErrorType` | Status |
| --- | --- | --- |
| `Error.Validation` | `Validation` | 400 |
| `Error.Unauthorized` | `Unauthorized` | 401 |
| `Error.Forbidden` | `Forbidden` | 403 |
| `Error.NotFound` | `NotFound` | 404 |
| `Error.Conflict` | `Conflict` | 409 |
| `Error.BusinessRule` | `BusinessRule` | 422 |

The mapping lives in one place (`ErrorTypeExtensions.ToStatusCode` in `Common/Results/Error.cs`) and is used
for both returned and thrown errors. Messages are sent to clients: write them for the client, never
include SQL, stack details or other users' data.

## 2. Returning errors (`Result`) — the default

Services return `Result` (no value) or `Result<T>`. An `Error` converts implicitly, so returning one is a
single line:

```csharp
public async Task<Result<ProductResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
{
    var product = await db.Products.AsNoTracking()
        .Where(p => p.Id == id)
        .Select(ProductMappings.ToResponseExpression)
        .FirstOrDefaultAsync(cancellationToken);

    return product is null ? ProductErrors.NotFound(id) : product;
}

public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
{
    ...
    return Result.Success();
}
```

The endpoint converts the result with `ToProblem()`:

```csharp
private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> GetById(
    int id, IProductService service, CancellationToken cancellationToken)
{
    var result = await service.GetByIdAsync(id, cancellationToken);
    return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
}
```

Why the default: the failure is part of the method's signature, callers have to handle it, and nothing
is thrown for normal outcomes.

## 3. Throwing errors

Throw when returning a `Result` through every layer would be noisy: deep helpers, domain entity methods,
or code called from many places. The global handler turns the exception into the same response.

**From a catalog:**

```csharp
throw ProductErrors.NotFound(id).ToException();      // 404, products.not_found — identical to returning it
```

**Ready-made exceptions** (`Common/Errors/Exceptions/`), for cases without a catalog entry:

| Exception | Status | Example |
| --- | --- | --- |
| `RequestValidationException` | 400 | `throw RequestValidationException.ForField("endDate", "Must be after startDate.");` |
| `UnauthorizedException` | 401 | `throw new UnauthorizedException();` |
| `ForbiddenException` | 403 | `throw new ForbiddenException("orders.not_owner", "You can only change your own orders.");` |
| `NotFoundException` | 404 | `throw NotFoundException.For("Product", id);` → `product.not_found` |
| `ConflictException` | 409 | `throw new ConflictException("orders.already_paid", "The order is already paid.");` |
| `BusinessRuleException` | 422 | `throw new BusinessRuleException("orders.too_late", "Orders can't be changed after shipping.");` |
| `ExternalServiceException` | 502 | `throw new ExternalServiceException("payments.failed", "The payment provider failed.", ex);` |
| `ExternalServiceException` | 503 | `... new ExternalServiceException("payments.unavailable", "...", ex, unavailable: true);` |

`RequestValidationException` with several fields:

```csharp
throw new RequestValidationException("orders.invalid", "The order is invalid.", new Dictionary<string, string[]>
{
    ["endDate"] = ["Must be after startDate."],
    ["items"] = ["At least one item is required."],
});
```

**External services:** always pass the original exception as the inner exception. It's logged with its
stack trace but never sent to the client:

```csharp
try
{
    await paymentClient.ChargeAsync(order, cancellationToken);
}
catch (HttpRequestException ex)
{
    throw new ExternalServiceException("payments.failed", "The payment could not be processed. Try again later.", ex);
}
```

Your own exception: derive from `AppException` with a fixed status, for example
`public sealed class QuotaExceededException(string message) : AppException(429, "quota_exceeded", message);`.

## 4. What the global handler does with everything else

`Common/Errors/GlobalExceptionHandler.cs` catches every unhandled exception; `ExceptionMapping.cs` decides
the answer:

| Exception | Status | `title` | Logged as |
| --- | --- | --- | --- |
| `AppException` (all of the above) | its own | its code | 4xx: information; 5xx: error + stack trace |
| Missing body, bad parameter (`BadHttpRequestException`) | 400 (or 413, 415, 408) | `bad_request` | warning, one line |
| Invalid JSON (`JsonException`) | 400 | `invalid_json` (detail includes `$.path`) | warning |
| Unique / foreign-key violation (PostgreSQL) | 409 | `conflict` | warning |
| `DbUpdateConcurrencyException` | 409 | `concurrency_conflict` | warning |
| Client disconnected | 499, no body | — | information |
| `NotImplementedException` | 501 | `not_implemented` | error |
| Anything else | 500 | `server_error` | error + stack trace |

When a library throws for something that is really a client error, add a case to
`ExceptionMapping.From` instead of catching it everywhere:

```csharp
// Common/Errors/ExceptionMapping.cs
TimeoutException =>
    new(StatusCodes.Status504GatewayTimeout, "timeout", "The operation timed out. Try again.", LogLevel.Warning),
```

## Rules

- Don't catch exceptions just to log and rethrow them; the handler logs once, with the correlation id.
- Don't return `500` yourself; throw or let it propagate.
- Don't put exception messages (`ex.Message`) into responses; only messages you wrote.
- Catch only where you can do something useful: retry, fall back, or translate (e.g. into
  `ExternalServiceException`).
- A failure that must not fail the request (like the welcome email) is caught and logged where it happens;
  see `UserService.SendWelcomeEmailAsync`.
- Test error paths: unit tests assert the returned `Error`, integration tests assert status and `title`.
