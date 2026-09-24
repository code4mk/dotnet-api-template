# Adding a feature

A feature is one folder under `src/DotnetApiTemplate.Api/Features/` with five files, plus its entity,
database configuration, migration and tests. This guide builds a **Categories** feature end to end;
`Features/Products/` is the reference to copy from.

```text
Domain/Entities/Category.cs                   1. entity
Data/Configurations/CategoryConfiguration.cs  2. table, lengths, indexes
Data/AppDbContext.cs                          3. DbSet
Data/Migrations/<timestamp>_AddCategories.cs  4. migration
Features/Categories/
├── CategoryDtos.cs                           5. request/response records
├── CategoryMappings.cs                       6. entity ↔ DTO
├── ICategoryService.cs                       7. service contract
├── CategoryService.cs                        8. business rules + CategoryErrors
└── CategoryEndpoints.cs                      9. HTTP
Common/Extensions/...                         10. register service, map endpoints
tests/...                                     11. unit + integration tests
```

## 1. Entity

```csharp
// Domain/Entities/Category.cs
using DotnetApiTemplate.Api.Domain.Common;

namespace DotnetApiTemplate.Api.Domain.Entities;

public class Category : BaseEntity              // Id, CreatedAt, UpdatedAt
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
```

## 2. Configuration

```csharp
// Data/Configurations/CategoryConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DotnetApiTemplate.Api.Domain.Entities;

namespace DotnetApiTemplate.Api.Data.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}
```

It's picked up automatically (`ApplyConfigurationsFromAssembly`).

## 3. DbSet

```csharp
// Data/AppDbContext.cs
public DbSet<Category> Categories => Set<Category>();
```

## 4. Migration

```bash
dotnet ef migrations add AddCategories --project src/DotnetApiTemplate.Api --output-dir Data/Migrations
# review Data/Migrations/<timestamp>_AddCategories.cs, then:
dotnet ef database update --project src/DotnetApiTemplate.Api
```

See [Database and migrations](database-and-migrations.md) for reviewing and data migrations.

## 5. DTOs

```csharp
// Features/Categories/CategoryDtos.cs
using System.ComponentModel.DataAnnotations;

namespace DotnetApiTemplate.Api.Features.Categories;

public sealed record CreateCategoryRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(1000)] string? Description);

public sealed record UpdateCategoryRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(1000)] string? Description);

public sealed record CategoryResponse(int Id, string Name, string? Description, DateTime CreatedAt, DateTime? UpdatedAt);
```

Validation attributes run automatically before the endpoint; invalid input returns `400` with `errors`.
Rules that need the database (unique name) go in the service.

## 6. Mappings

```csharp
// Features/Categories/CategoryMappings.cs
using System.Linq.Expressions;
using DotnetApiTemplate.Api.Domain.Entities;

namespace DotnetApiTemplate.Api.Features.Categories;

public static class CategoryMappings
{
    /// <summary>For EF queries: translated to SQL, only the needed columns are read.</summary>
    public static readonly Expression<Func<Category, CategoryResponse>> ToResponseExpression = c =>
        new CategoryResponse(c.Id, c.Name, c.Description, c.CreatedAt, c.UpdatedAt);

    private static readonly Func<Category, CategoryResponse> ToResponseFunc = ToResponseExpression.Compile();

    /// <summary>For an entity already in memory.</summary>
    public static CategoryResponse ToResponse(this Category category) => ToResponseFunc(category);

    public static Category ToEntity(this CreateCategoryRequest request) => new()
    {
        Name = request.Name.Trim(),
        Description = request.Description?.Trim(),
    };

    public static void ApplyTo(this UpdateCategoryRequest request, Category category)
    {
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
    }
}
```

## 7. Service contract

```csharp
// Features/Categories/ICategoryService.cs
using DotnetApiTemplate.Api.Common.Pagination;
using DotnetApiTemplate.Api.Common.Results;

namespace DotnetApiTemplate.Api.Features.Categories;

public interface ICategoryService
{
    Task<PagedResponse<CategoryResponse>> GetAllAsync(int? page, int? pageSize, CancellationToken cancellationToken);

    Task<Result<CategoryResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken);

    Task<Result<CategoryResponse>> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
```

## 8. Service and errors

```csharp
// Features/Categories/CategoryService.cs
using Microsoft.EntityFrameworkCore;
using DotnetApiTemplate.Api.Common.Pagination;
using DotnetApiTemplate.Api.Common.Results;
using DotnetApiTemplate.Api.Data;

namespace DotnetApiTemplate.Api.Features.Categories;

internal sealed class CategoryService(AppDbContext db) : ICategoryService
{
    public Task<PagedResponse<CategoryResponse>> GetAllAsync(int? page, int? pageSize, CancellationToken cancellationToken) =>
        db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(CategoryMappings.ToResponseExpression)
            .ToPagedResponseAsync(page, pageSize, cancellationToken);

    public async Task<Result<CategoryResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(CategoryMappings.ToResponseExpression)
            .FirstOrDefaultAsync(cancellationToken);

        return category is null ? CategoryErrors.NotFound(id) : category;
    }

    public async Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == name, cancellationToken))
        {
            return CategoryErrors.NameAlreadyExists(name);
        }

        var category = request.ToEntity();
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return category.ToResponse();
    }

    public async Task<Result<CategoryResponse>> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound(id);
        }

        request.ApplyTo(category);
        await db.SaveChangesAsync(cancellationToken);

        return category.ToResponse();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound(id);
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>All business errors of the Categories feature.</summary>
public static class CategoryErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("categories.not_found", $"Category with id {id} was not found.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("categories.name_exists", $"A category named '{name}' already exists.");
}
```

Services return `Result` for expected failures and never touch `HttpContext`. See
[Errors and exceptions](errors-and-exceptions.md).

## 9. Endpoints

```csharp
// Features/Categories/CategoryEndpoints.cs
using DotnetApiTemplate.Api.Common.Pagination;
using DotnetApiTemplate.Api.Common.Results;
using DotnetApiTemplate.Api.Infrastructure.Authentication;

namespace DotnetApiTemplate.Api.Features.Categories;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/categories").WithTags("Categories");

        group.MapGet("/", GetAll)
            .WithName("GetCategories")
            .WithSummary("List categories (paged).")
            .AllowAnonymous();

        group.MapGet("/{id:int}", GetById)
            .WithName("GetCategoryById")
            .WithSummary("Get one category.")
            .AllowAnonymous();

        group.MapPost("/", Create)
            .WithName("CreateCategory")
            .WithSummary("Create a category.")
            .ProducesValidationProblem();

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateCategory")
            .WithSummary("Update a category.")
            .ProducesValidationProblem();

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteCategory")
            .WithSummary("Delete a category (admin only).")
            .RequireAuthorization(Policies.Admin);

        return api;
    }

    private static async Task<Ok<PagedResponse<CategoryResponse>>> GetAll(
        int? page, int? pageSize, ICategoryService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAllAsync(page, pageSize, cancellationToken));

    private static async Task<Results<Ok<CategoryResponse>, ProblemHttpResult>> GetById(
        int id, ICategoryService service, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<CreatedAtRoute<CategoryResponse>, ProblemHttpResult>> Create(
        CreateCategoryRequest request, ICategoryService service, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetCategoryById", new { id = result.Value.Id })   // 201 + Location
            : result.ToProblem();
    }

    private static async Task<Results<Ok<CategoryResponse>, ProblemHttpResult>> Update(
        int id, UpdateCategoryRequest request, ICategoryService service, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        int id, ICategoryService service, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
```

Endpoints are thin: bind, call one service method, convert the result. Everything under `/api` requires a
token unless it says `AllowAnonymous()`; see [Authentication](authentication-and-authorization.md).

## 10. Register and map

```csharp
// Common/Extensions/ServiceCollectionExtensions.cs → AddFeatures()
services.AddScoped<ICategoryService, CategoryService>();

// Common/Extensions/EndpointExtensions.cs → MapFeatures()
api.MapCategoryEndpoints();
```

Run the API and the feature shows up in Swagger (`/swagger`) under **Categories**.

## 11. Tests

```csharp
// tests/DotnetApiTemplate.UnitTests/Features/Categories/CategoryServiceTests.cs
using DotnetApiTemplate.Api.Data;
using DotnetApiTemplate.Api.Features.Categories;
using DotnetApiTemplate.UnitTests.TestUtilities;

namespace DotnetApiTemplate.UnitTests.Features.Categories;

public sealed class CategoryServiceTests : IDisposable
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly CategoryService _sut;

    public CategoryServiceTests() => _sut = new CategoryService(_db);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ReturnsConflict()
    {
        await _sut.CreateAsync(new CreateCategoryRequest("Hardware", null), CancellationToken.None);

        var result = await _sut.CreateAsync(new CreateCategoryRequest("Hardware", null), CancellationToken.None);

        Assert.Equal("categories.name_exists", result.Error!.Code);
    }
}

// tests/DotnetApiTemplate.IntegrationTests/Features/Categories/CategoryEndpointsTests.cs
using System.Net;

namespace DotnetApiTemplate.IntegrationTests.Features.Categories;

public sealed class CategoryEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task DeleteCategory_AsUser_ReturnsForbidden()
    {
        var response = await factory.CreateAuthenticatedClient("User").DeleteAsync("/api/categories/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

See [Testing](testing.md) for what to cover (success, `401`, `403`, `400`, each business error).

## Finally

- Add sample requests to `DotnetApiTemplate.Api.http`.
- New settings go into `.env.example` ([Configuration](configuration-and-environments.md)).
- Walk through the [PR checklist](coding-guidelines.md#pull-request-checklist).
