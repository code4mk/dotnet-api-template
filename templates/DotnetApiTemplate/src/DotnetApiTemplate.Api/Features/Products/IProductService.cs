using DotnetApiTemplate.Api.Common.Pagination;
using DotnetApiTemplate.Api.Common.Results;

namespace DotnetApiTemplate.Api.Features.Products;

public interface IProductService
{
    Task<PagedResponse<ProductResponse>> GetAllAsync(string? search, int? page, int? pageSize, CancellationToken cancellationToken);

    Task<Result<ProductResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);

    Task<Result<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
