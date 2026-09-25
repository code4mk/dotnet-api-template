using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DotnetApiTemplate.Api.Common.Pagination;
using DotnetApiTemplate.Api.Common.Results;
using DotnetApiTemplate.Api.Data;
using DotnetApiTemplate.Api.Domain.Entities;
using DotnetApiTemplate.Api.Features.Users.Jobs;

namespace DotnetApiTemplate.Api.Features.Users;

internal sealed class UserService(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    IBackgroundJobClient jobs) : IUserService
{
    public Task<PagedResponse<UserResponse>> GetAllAsync(int? page, int? pageSize, CancellationToken cancellationToken) =>
        db.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(UserMappings.ToResponseExpression)
            .ToPagedResponseAsync(page, pageSize, cancellationToken);

    public async Task<Result<UserResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(UserMappings.ToResponseExpression)
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? UserErrors.NotFound(id) : user;
    }

    public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var email = UserMappings.NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return UserErrors.EmailAlreadyExists(email);
        }

        var user = request.ToEntity();
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        // In the background: sign-up doesn't wait for SMTP, and failures are retried (see SendWelcomeEmailJob).
        jobs.Enqueue<SendWelcomeEmailJob>(job => job.ExecuteAsync(user.Id, CancellationToken.None));

        return user.ToResponse();
    }

    public async Task<Result<UserResponse>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(id);
        }

        request.ApplyTo(user);
        await db.SaveChangesAsync(cancellationToken);

        return user.ToResponse();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(id);
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>All business errors of the Users feature, in one place.</summary>
public static class UserErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("users.not_found", $"User with id {id} was not found.");

    public static Error EmailAlreadyExists(string email) =>
        Error.Conflict("users.email_exists", $"A user with email '{email}' already exists.");
}
