using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotnetApiTemplate.Api.Data;
using Hangfire;
using DotnetApiTemplate.Api.Infrastructure.Email.Sending;
using DotnetApiTemplate.IntegrationTests.TestUtilities;

namespace DotnetApiTemplate.IntegrationTests;

/// <summary>
/// Starts the real API in memory with an in-memory database and a fake authentication scheme.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"integration-tests-{Guid.NewGuid()}";

    /// <summary>Emails "sent" by the API: rendered for real, but captured instead of delivered.</summary>
    public FakeEmailSender Emails { get; } = new();

    /// <summary>Jobs enqueued by the API: recorded, not stored. Run them with <c>Jobs.RunAsync(factory.Services)</c>.</summary>
    public FakeBackgroundJobClient Jobs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Required settings (validated on startup).
        builder.UseSetting("APP_ROLE", "api");   // no Hangfire job server in tests: jobs are recorded, see Jobs
        builder.UseSetting("JWT_ISSUER", "tests");
        builder.UseSetting("JWT_AUDIENCE", "tests");
        builder.UseSetting("JWT_SIGNING_KEY", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("DB_NAME", "tests");
        builder.UseSetting("DB_USER", "tests");
        builder.UseSetting("DB_PASSWORD", "tests");

        builder.ConfigureTestServices(services =>
        {
            // Replace PostgreSQL with an in-memory database.
            var dbDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)))
                .ToList();

            foreach (var descriptor in dbDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            services.RemoveAll<IBackgroundJobClient>();
            services.AddSingleton<IBackgroundJobClient>(Jobs);

            // Replace JWT with a simple test scheme (see TestAuthHandler).
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>A client that is authenticated as a user with the given role.</summary>
    public HttpClient CreateAuthenticatedClient(string role = "Admin")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        return client;
    }
}
