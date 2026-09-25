using Microsoft.Extensions.Logging.Abstractions;
using DotnetApiTemplate.Api.Data;
using DotnetApiTemplate.Api.Domain.Entities;
using DotnetApiTemplate.Api.Features.Products.Jobs;
using DotnetApiTemplate.UnitTests.TestUtilities;

namespace DotnetApiTemplate.UnitTests.Features.Products;

public sealed class LowStockReportJobTests : IDisposable
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ExecuteAsync_CountsProductsBelowThreshold()
    {
        _db.Products.AddRange(
            new Product { Name = "Keyboard", Price = 10, Stock = 2 },
            new Product { Name = "Mouse", Price = 5, Stock = LowStockReportJob.Threshold },
            new Product { Name = "Cable", Price = 1, Stock = 0 });
        await _db.SaveChangesAsync();

        var count = await new LowStockReportJob(_db, NullLogger<LowStockReportJob>.Instance).ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, count);
    }
}
