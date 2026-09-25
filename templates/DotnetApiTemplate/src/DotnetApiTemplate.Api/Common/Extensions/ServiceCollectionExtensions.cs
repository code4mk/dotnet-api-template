using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotnetApiTemplate.Api.Common.Cors;
using DotnetApiTemplate.Api.Common.Errors;
using DotnetApiTemplate.Api.Common.Features;
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
using DotnetApiTemplate.Api.Infrastructure.Jobs;

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

    /// <summary>Database, authentication, email, background jobs and other external dependencies.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(sp.GetRequiredService<DatabaseSettings>().ConnectionString);

            if (EF.IsDesignTime)
            {
                // `dotnet ef` only: no misleading "Failed executing DbCommand" on the first database update.
                options.AddInterceptors(new MissingHistoryTableInterceptor());
            }
        });

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddJwtAuthentication(configuration);
        services.AddEmail(configuration);
        services.AddJobs(configuration);

        return services;
    }

    /// <summary>
    /// Feature services. Manual mode (default): one line per service. With
    /// <see cref="FeatureDiscovery.AutoDiscovery"/>, every <c>XService : IXService</c> in Features/ is added (scoped).
    /// </summary>
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        if (FeatureDiscovery.AutoDiscovery)
        {
            foreach (var (service, implementation) in FeatureDiscovery.ServiceTypes)
            {
                services.TryAddScoped(service, implementation);
            }
        }
        else
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProductService, ProductService>();
        }

        // Both modes: register here what doesn't fit the XService : IXService convention
        // (singletons, classes with several interfaces, ...).

        return services;
    }
}
