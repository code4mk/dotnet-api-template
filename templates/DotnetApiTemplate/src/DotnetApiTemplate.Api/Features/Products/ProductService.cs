using Microsoft.EntityFrameworkCore;
using DotnetApiTemplate.Api.Common.Pagination;
using DotnetApiTemplate.Api.Common.Results;
using DotnetApiTemplate.Api.Data;

namespace DotnetApiTemplate.Api.Features.Products;

internal sealed class ProductService(AppDbContext db) : IProductService
{
    public Task<PagedResponse<ProductResponse>> GetAllAsync(
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        return query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Select(ProductMappings.ToResponseExpression)
            .ToPagedResponseAsync(page, pageSize, cancellationToken);
    }

    public async Task<Result<ProductResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(ProductMappings.ToResponseExpression)
            .FirstOrDefaultAsync(cancellationToken);

        return product is null ? ProductErrors.NotFound(id) : product;
    }

    public async Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await db.Products.AnyAsync(p => p.Name == name, cancellationToken))
        {
            return ProductErrors.NameAlreadyExists(name);
        }

        var product = request.ToEntity();
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        return product.ToResponse();
    }

    public async Task<Result<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(id);
        }

        var name = request.Name.Trim();
        if (await db.Products.AnyAsync(p => p.Id != id && p.Name == name, cancellationToken))
        {
            return ProductErrors.NameAlreadyExists(name);
        }

        request.ApplyTo(product);
        await db.SaveChangesAsync(cancellationToken);

        return product.ToResponse();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(id);
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>All business errors of the Products feature, in one place.</summary>
public static class ProductErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("products.not_found", $"Product with id {id} was not found.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("products.name_exists", $"A product named '{name}' already exists.");
}
