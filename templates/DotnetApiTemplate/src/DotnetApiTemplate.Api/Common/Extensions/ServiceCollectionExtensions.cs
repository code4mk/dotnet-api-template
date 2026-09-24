using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotnetApiTemplate.Api.Common.Cors;
using DotnetApiTemplate.Api.Common.Errors;
using DotnetApiTemplate.Api.Common.Json;
using DotnetApiTemplate.Api.Common.Middleware;
using DotnetApiTemplate.Api.Common.OpenApi;
using DotnetApiTemplate.Api.Common.Settings;
using DotnetApiTemplate.Api.Data;
using DotnetApiTemplate.Api.Domain.Entities;
using DotnetApiTemplate.Api.Features.Auth;
using DotnetApiTemplate.Api.Features.Products;
using DotnetApiTemplate.Api.Features.Users;
using DotnetApiTemplate.Api.Infrastructure.Authentication;
using DotnetApiTemplate.Api.Infrastructure.Email;

namespace DotnetApiTemplate.Api.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Cross-cutting API setup: errors, validation, OpenAPI, JSON, CORS.</summary>
    public static IServiceCollection AddApiDefaults(this IServiceCollection services, IConfiguration configuration)
    {
        // Every error response (exceptions, validation, Result errors, 404s) is ProblemDetails
        // and carries the request's correlation id, the same value as the X-Correlation-Id header.
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??= $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions[CorrelationIdMiddleware.ProblemDetailsKey] = context.HttpContext.TraceIdentifier;
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Throw on bad requests (missing/invalid body, bad parameters) in every environment, not only
        // Development, so GlobalExceptionHandler answers them with a consistent 400 ProblemDetails.
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        services.AddValidation();
        services.AddApiDocs();
        services.AddHealthChecks();

        // Request/response bodies, ProblemDetails and OpenAPI all use these settings (see JsonDefaults).
        services.ConfigureHttpJsonOptions(options => JsonDefaults.Configure(options.SerializerOptions));

        services.AddApiCors(configuration);                 // CORS_ALLOWED_ORIGINS

        return services;
    }

    /// <summary>Database, authentication, email and other external dependencies.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddEnvSettings<AppSettings>(configuration);
        services.AddEnvSettings<DatabaseSettings>(configuration);

        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<DatabaseSettings>().ConnectionString));

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddJwtAuthentication(configuration);
        services.AddEmail(configuration);

        return services;
    }

    /// <summary>Feature services. Add one line per new feature.</summary>
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
