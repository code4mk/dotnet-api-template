using DotnetApiTemplate.Api.Common.Pagination;
using DotnetApiTemplate.Api.Common.Results;
using DotnetApiTemplate.Api.Infrastructure.Authentication;

namespace DotnetApiTemplate.Api.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/users")
            .WithTags("Users");

        group.MapGet("/", GetAll)
            .WithName("GetUsers")
            .WithSummary("List users (paged).");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetUserById")
            .WithSummary("Get one user.");

        group.MapPost("/", Create)
            .WithName("CreateUser")
            .WithSummary("Register a new user.")
            .AllowAnonymous()
            .ProducesValidationProblem();

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateUser")
            .WithSummary("Update a user.")
            .ProducesValidationProblem();

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteUser")
            .WithSummary("Delete a user (admin only).")
            .RequireAuthorization(Policies.Admin);

        return api;
    }

    private static async Task<Ok<PagedResponse<UserResponse>>> GetAll(
        IUserService service,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var users = await service.GetAllAsync(page, pageSize, cancellationToken);
        return TypedResults.Ok(users);
    }

    private static async Task<Results<Ok<UserResponse>, ProblemHttpResult>> GetById(
        int id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<CreatedAtRoute<UserResponse>, ProblemHttpResult>> Create(
        CreateUserRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetUserById", new { id = result.Value.Id })
            : result.ToProblem();
    }

    private static async Task<Results<Ok<UserResponse>, ProblemHttpResult>> Update(
        int id,
        UpdateUserRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        int id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
