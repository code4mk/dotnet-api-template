using System.ComponentModel.DataAnnotations;
using DotnetApiTemplate.Api.Domain.Enums;

namespace DotnetApiTemplate.Api.Features.Users;

// Requests: validation attributes are checked automatically before the endpoint runs.

public sealed record CreateUserRequest(
    [Required, StringLength(100)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password);

public sealed record UpdateUserRequest(
    [Required, StringLength(100)] string FullName,
    bool IsActive);

// Responses

public sealed record UserResponse(
    int Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
