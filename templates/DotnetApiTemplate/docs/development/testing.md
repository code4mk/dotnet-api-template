# Testing

```bash
dotnet test                                              # everything
dotnet test tests/DotnetApiTemplate.UnitTests            # one project
dotnet test --filter "FullyQualifiedName~ProductService" # one class (or part of a name)
dotnet test --collect:"XPlat Code Coverage"              # with coverage (as CI does)
```

Tests need no database, SMTP server or `.env`: they run anywhere, including CI.

| Project | Tests | Speed |
| --- | --- | --- |
| `tests/DotnetApiTemplate.UnitTests` | Services, renderers, settings, JSON, error mapping; EF Core in-memory database | milliseconds |
| `tests/DotnetApiTemplate.IntegrationTests` | Real HTTP requests through the whole API (routing, validation, auth, errors, JSON) with `WebApplicationFactory` | fast, in-memory |

Mirror the source folders: tests for `Features/Products/ProductService.cs` go in
`UnitTests/Features/Products/ProductServiceTests.cs`.

## Naming

`Method_Condition_ExpectedResult`, one behavior per test, arrange / act / assert separated by blank lines:

```csharp
[Fact]
public async Task CreateAsync_WithDuplicateName_ReturnsConflict()
```

## Unit tests (services)

Create the service with an in-memory database and fakes for its dependencies:

```csharp
public sealed class ProductServiceTests : IDisposable
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();   // fresh database per test
    private readonly ProductService _sut;

    public ProductServiceTests() => _sut = new ProductService(_db);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNotFound()
    {
        var result = await _sut.GetByIdAsync(42, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("products.not_found", result.Error!.Code);
    }
}
```

Test utilities (`UnitTests/TestUtilities/`):

| Utility | Use |
| --- | --- |
| `TestDbContextFactory.Create()` | An isolated in-memory `AppDbContext` |
| `FakeEmailService` | Records sent emails: `Assert.Single(_emails.Sent)` |
| `TestHostEnvironment("Development")` | An `IHostEnvironment` for code that checks the environment |
| `NullLogger<T>.Instance` | A logger that does nothing |
| `Options.Create(new XSettings { ... })` | Settings for classes that take `IOptions<T>` |

For time-dependent code, inject `TimeProvider` in the service and pass a fake
(`Microsoft.Extensions.TimeProvider.Testing`'s `FakeTimeProvider`) in tests.

**In-memory database limits:** no unique constraints, foreign keys, transactions or SQL. Tests that depend
on these (a unique index, a raw SQL query) belong in integration tests against real PostgreSQL, e.g. with
[Testcontainers](https://dotnet.testcontainers.org/).

## Integration tests (endpoints)

`ApiFactory` starts the real API in memory and replaces the outside world:

| Replaced | With |
| --- | --- |
| PostgreSQL | EF Core in-memory database (one per factory) |
| JWT authentication | `TestAuthHandler`: the `X-Test-Role` header logs you in with that role |
| Email sending | `FakeEmailSender`: emails are rendered for real, then captured in `factory.Emails.Sent` |
| Environment | `Testing` (no Swagger), fixed JWT and database settings |

```csharp
public sealed class ProductEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task CreateProduct_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();                         // anonymous

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest("Pen", null, 1.5m, 10));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AsUser_ReturnsCreated()
    {
        var client = factory.CreateAuthenticatedClient("User");     // logged in as a User

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest("Pen", null, 1.5m, 10));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }
}
```

For every endpoint, cover: the success case, `401` without a token, `403` for the wrong role (if it has a
policy), `400` validation, and each business error (`404`, `409`, ...). Assert the error `title`:

```csharp
var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
Assert.Equal("products.name_exists", problem!.Title);
```

Tips:

- **Unique data per test.** Tests in a class share one factory and database: use
  `$"jane-{Guid.NewGuid():N}@example.com"`, not a fixed email.
- **Responses with enums:** read them with the API's options, `ReadFromJsonAsync<UserResponse>(JsonDefaults.Options)`,
  because enums are sent as strings.
- **Emails:** `Assert.Single(factory.Emails.Sent, m => m.To == email)` and check `Subject`, `HtmlBody`, `TextBody`.
- **Different settings or environment for one test:**
  `factory.WithWebHostBuilder(b => b.UseSetting("CORS_ALLOWED_ORIGINS", "...")).CreateClient()` or
  `b.UseEnvironment("Development")` (see `CorsTests`, `OpenApiTests`).
- **Replace a service:** in `ApiFactory.ConfigureTestServices`, `services.RemoveAll<IPaymentClient>();`
  then `services.AddSingleton<IPaymentClient>(new FakePaymentClient());`.

## Route snapshot and wiring tests

Two guards run in every project, in both wiring modes ([manual or auto-discovery](adding-a-feature.md#10-register-and-map)):

- `FeatureWiringTests` (unit): every `I...Service` under `Features/` is registered as scoped.
- `RouteTests` (integration): every `IEndpoints` class is mapped, and the route table matches
  `Common/routes.snapshot.txt` (method, route, `public` / `auth` / `policy:X`, feature).

When you add, remove or change the access of a route, the snapshot test fails and prints the new list.
Review it, then update and commit the file:

```bash
UPDATE_SNAPSHOTS=1 dotnet test --filter "FullyQualifiedName~RouteTests"
```

The snapshot makes every new public endpoint visible in code review.

## What to test where

| Behavior | Test type |
| --- | --- |
| Business rules, error codes, calculations | Unit (service) |
| Status codes, auth, validation, JSON shape, headers | Integration |
| Email content | Unit (renderer) + integration (sent at the right moment) |
| Migrations and SQL | Apply them to a real database (Testcontainers or a CI job) |

## CI

`.github/workflows/build-and-test.yml` restores, builds in Release and runs all tests with coverage on
every push and pull request to `main`; test results are uploaded as an artifact.
