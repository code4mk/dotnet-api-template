using Microsoft.EntityFrameworkCore;
using DotnetApiTemplate.Api.Data;

namespace DotnetApiTemplate.UnitTests.TestUtilities;

internal static class TestDbContextFactory
{
    /// <summary>Creates a fresh, isolated in-memory database for each test.</summary>
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"unit-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
