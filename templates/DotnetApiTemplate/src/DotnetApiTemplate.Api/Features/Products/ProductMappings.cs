using System.Linq.Expressions;
using DotnetApiTemplate.Api.Domain.Entities;

namespace DotnetApiTemplate.Api.Features.Products;

public static class ProductMappings
{
    /// <summary>Use in EF queries: <c>db.Products.Select(ProductMappings.ToResponseExpression)</c>.</summary>
    public static readonly Expression<Func<Product, ProductResponse>> ToResponseExpression = product => new ProductResponse(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.Stock,
        product.CreatedAt,
        product.UpdatedAt);

    private static readonly Func<Product, ProductResponse> ToResponseFunc = ToResponseExpression.Compile();

    /// <summary>Use for an entity already loaded in memory.</summary>
    public static ProductResponse ToResponse(this Product product) => ToResponseFunc(product);

    public static Product ToEntity(this CreateProductRequest request) => new()
    {
        Name = request.Name.Trim(),
        Description = request.Description?.Trim(),
        Price = request.Price,
        Stock = request.Stock
    };

    public static void ApplyTo(this UpdateProductRequest request, Product product)
    {
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;
    }
}
