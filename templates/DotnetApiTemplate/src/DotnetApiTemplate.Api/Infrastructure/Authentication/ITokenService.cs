using DotnetApiTemplate.Api.Domain.Entities;

namespace DotnetApiTemplate.Api.Infrastructure.Authentication;

public interface ITokenService
{
    AccessToken CreateToken(User user);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);
