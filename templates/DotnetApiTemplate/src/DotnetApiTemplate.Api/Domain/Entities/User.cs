using DotnetApiTemplate.Api.Domain.Common;
using DotnetApiTemplate.Api.Domain.Enums;

namespace DotnetApiTemplate.Api.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Always stored in lower case.</summary>
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public bool IsActive { get; set; } = true;
}
