using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DotnetApiTemplate.Api.Domain.Entities;
using DotnetApiTemplate.Api.Domain.Enums;

namespace DotnetApiTemplate.Api.Data.Seed;

/// <summary>Development-only database setup and seed data.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        if (db.Database.IsRelational() && db.Database.GetMigrations().Any())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        await SeedAdminAsync(db, hasher, app.Configuration, logger, cancellationToken);
        await SeedProductsAsync(db, cancellationToken);
    }

    private static async Task SeedAdminAsync(
        AppDbContext db,
        IPasswordHasher<User> hasher,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        email = email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return;
        }

        var admin = new User { FullName = "Administrator", Email = email, Role = UserRole.Admin };
        admin.PasswordHash = hasher.HashPassword(admin, password);

        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded admin user {Email}", email);
    }

    private static async Task SeedProductsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Products.AddRange(
            new Product { Name = "Laptop", Description = "14-inch business laptop", Price = 899.00m, Stock = 10 },
            new Product { Name = "Mouse", Description = "Wireless mouse", Price = 19.99m, Stock = 150 },
            new Product { Name = "Monitor", Description = "27-inch 4K monitor", Price = 329.50m, Stock = 25 });

        await db.SaveChangesAsync(cancellationToken);
    }
}
