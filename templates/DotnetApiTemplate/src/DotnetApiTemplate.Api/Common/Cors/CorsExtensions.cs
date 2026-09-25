using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using DotnetApiTemplate.Api.Common.Middleware;
using DotnetApiTemplate.Api.Common.Settings;

namespace DotnetApiTemplate.Api.Common.Cors;

public static class CorsExtensions
{
    /// <summary>
    /// CORS for browser frontends on other origins. Only the exact origins in CORS_ALLOWED_ORIGINS are
    /// allowed; with none configured, no CORS headers are sent and browsers block cross-origin calls.
    /// No credentials (cookies): the API authenticates with the Authorization header.
    /// </summary>
    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors();

        services.AddOptions<CorsOptions>()
            .Configure<IOptions<CorsSettings>>((cors, settings) =>
            {
                var origins = settings.Value.Origins;
                if (origins.Count == 0)
                {
                    return;
                }

                cors.AddDefaultPolicy(policy => policy
                    .WithOrigins([.. origins])
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                    .WithHeaders("Authorization", "Content-Type", "Accept", CorrelationIdMiddleware.HeaderName)
                    // Readable by frontend code: the correlation id (for error reports) and Location (after 201 Created).
                    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName, "Location")
                    // Browsers cache the preflight (OPTIONS) answer instead of sending it before every request.
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
            });

        return services;
    }
}
