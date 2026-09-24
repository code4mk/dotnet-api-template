using System.ComponentModel.DataAnnotations;

namespace DotnetApiTemplate.Api.Features.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAt);
