using System.Linq.Expressions;
using DotnetApiTemplate.Api.Domain.Entities;

namespace DotnetApiTemplate.Api.Features.Users;

public static class UserMappings
{
    /// <summary>Use in EF queries: <c>db.Users.Select(UserMappings.ToResponseExpression)</c>.</summary>
    public static readonly Expression<Func<User, UserResponse>> ToResponseExpression = user => new UserResponse(
        user.Id,
        user.FullName,
        user.Email,
        user.Role,
        user.IsActive,
        user.CreatedAt,
        user.UpdatedAt);

    private static readonly Func<User, UserResponse> ToResponseFunc = ToResponseExpression.Compile();

    /// <summary>Use for an entity already loaded in memory.</summary>
    public static UserResponse ToResponse(this User user) => ToResponseFunc(user);

    public static User ToEntity(this CreateUserRequest request) => new()
    {
        FullName = request.FullName.Trim(),
        Email = NormalizeEmail(request.Email)
    };

    public static void ApplyTo(this UpdateUserRequest request, User user)
    {
        user.FullName = request.FullName.Trim();
        user.IsActive = request.IsActive;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
