using DotnetApiTemplate.Api.Common.Pagination;
using DotnetApiTemplate.Api.Common.Results;

namespace DotnetApiTemplate.Api.Features.Users;

public interface IUserService
{
    Task<PagedResponse<UserResponse>> GetAllAsync(int? page, int? pageSize, CancellationToken cancellationToken);

    Task<Result<UserResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<Result<UserResponse>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
