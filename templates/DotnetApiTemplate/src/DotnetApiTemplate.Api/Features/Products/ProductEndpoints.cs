using DotnetApiTemplate.Api.Common.Pagination;
using DotnetApiTemplate.Api.Common.Results;

namespace DotnetApiTemplate.Api.Features.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/products")
            .WithTags("Products");

        group.MapGet("/", GetAll)
            .WithName("GetProducts")
            .WithSummary("List products (paged, optional name search).")
            .AllowAnonymous();

        group.MapGet("/{id:int}", GetById)
            .WithName("GetProductById")
            .WithSummary("Get one product.")
            .AllowAnonymous();

        group.MapPost("/", Create)
            .WithName("CreateProduct")
            .WithSummary("Create a product.")
            .ProducesValidationProblem();

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateProduct")
            .WithSummary("Update a product.")
            .ProducesValidationProblem();

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product.");

        return api;
    }

    private static async Task<Ok<PagedResponse<ProductResponse>>> GetAll(
        IProductService service,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var products = await service.GetAllAsync(search, page, pageSize, cancellationToken);
        return TypedResults.Ok(products);
    }

    private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> GetById(
        int id,
        IProductService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<CreatedAtRoute<ProductResponse>, ProblemHttpResult>> Create(
        CreateProductRequest request,
        IProductService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetProductById", new { id = result.Value.Id })
            : result.ToProblem();
    }

    private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> Update(
        int id,
        UpdateProductRequest request,
        IProductService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        int id,
        IProductService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
