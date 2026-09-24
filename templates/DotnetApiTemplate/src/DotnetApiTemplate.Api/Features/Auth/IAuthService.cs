using DotnetApiTemplate.Api.Common.Results;

namespace DotnetApiTemplate.Api.Features.Auth;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
