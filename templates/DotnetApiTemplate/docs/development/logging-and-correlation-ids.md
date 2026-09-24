# Logging and correlation ids

## Correlation ids

Every request gets an id that ties together the response, the error body and all log lines:

- If the client sends `X-Correlation-Id` (up to 64 characters), it's reused; otherwise a new one is created.
- It's returned in the `X-Correlation-Id` response header, and as `correlationId` in every error response.
- It's in the logging scope, so every log line written during the request carries it.
- Browser frontends can read the header (it's exposed by CORS).

`Common/Middleware/CorrelationIdMiddleware.cs` does this; it runs first in the pipeline.

**Investigating a reported error:** ask for the `correlationId` from the error response (or the header),
then search the logs for it. Frontends should show it in error messages ("Reference: 67fd…"), and services
calling other services should forward the header.

## Writing logs

Inject `ILogger<T>` and use **message templates**, not string interpolation:

```csharp
internal sealed class OrderService(AppDbContext db, ILogger<OrderService> logger) : IOrderService
{
    public async Task<Result> CancelAsync(int orderId, CancellationToken cancellationToken)
    {
        ...
        logger.LogInformation("Order {OrderId} cancelled by user {UserId}", orderId, userId);   // ✅
        // logger.LogInformation($"Order {orderId} cancelled");                                 // ❌
    }
}
```

Templates keep `OrderId` as a searchable field in structured log systems, and skip the formatting work
when the level is disabled.

| Level | Use for |
| --- | --- |
| `Trace` / `Debug` | Detail for diagnosing a problem locally; off by default |
| `Information` | Business events worth keeping: order placed, user registered, email sent |
| `Warning` | Something unexpected that the app handled: retry succeeded, fallback used, client error |
| `Error` | An operation failed and needs attention |
| `Critical` | The app can't continue (database down at startup) |

Rules:

- **Never log secrets or personal data**: passwords, tokens, full card numbers, and avoid logging emails
  and names where an id is enough.
- **Don't log and rethrow.** Unhandled exceptions are logged once by the global handler, with the stack
  trace and the correlation id. See [Errors and exceptions](errors-and-exceptions.md).
- Pass the exception as the first argument so the stack trace is kept:
  `logger.LogError(ex, "Payment for order {OrderId} failed", orderId);`

## What the template logs for you

| Event | Level |
| --- | --- |
| Client errors (bad JSON, missing body, thrown 4xx) | Warning / Information, one line, no stack trace |
| Validation failures (`400` with `errors`) | not logged (answered by the framework) |
| Unexpected exceptions (500) and external service failures (5xx) | Error, with stack trace |
| Emails when `EMAIL_HOST` is empty | Information (with the text body) |
| SQL commands (Development only) | Information, from `Microsoft.EntityFrameworkCore.Database.Command` |

## Configuring levels

`appsettings.json` (all environments) and `appsettings.Development.json` (Development):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
```

More detail for one area: add its namespace (the logger category) under `LogLevel`:

```json
"DotnetApiTemplate.Api.Features.Orders": "Debug"
```

Or override without changing files, with an environment variable (`__` separates sections):

```bash
Logging__LogLevel__Default=Debug dotnet run --project src/DotnetApiTemplate.Api
```

## Production

Logs go to the console (stdout), which Docker and most platforms collect. For central search, switch the
console output to JSON so every field is indexed:

```csharp
// Program.cs, after CreateBuilder
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);   // includes CorrelationId
```

or plug in a provider such as OpenTelemetry, Serilog or Application Insights.

## Health check

`GET /health` returns `200 Healthy` while the app is running. Use it for container health checks and load
balancers. It doesn't check the database; add `services.AddHealthChecks().AddNpgSql(...)` (package
`AspNetCore.HealthChecks.NpgSql`) if you want it to.
