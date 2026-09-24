using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using DotnetApiTemplate.Api.Domain.Entities;

namespace DotnetApiTemplate.Api.Infrastructure.Authentication;

internal sealed class JwtTokenService(IOptions<JwtSettings> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JwtSettings _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateToken(User user)
    {
        var expiresAt = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(_options.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(AppClaimTypes.UserId, user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(AppClaimTypes.Email, user.Email),
                new Claim(AppClaimTypes.Name, user.FullName),
                new Claim(AppClaimTypes.Role, user.Role.ToString())
            ])
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }
}
