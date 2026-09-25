using Hangfire;
using Microsoft.EntityFrameworkCore;
using DotnetApiTemplate.Api.Data;

namespace DotnetApiTemplate.Api.Features.Products.Jobs;

/// <summary>
/// Sample recurring job (daily, see Infrastructure/Jobs/RecurringJobs.cs): logs products that are running low.
/// Replace the log line with an email or a notification in a real project.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 600)]   // never two runs at the same time, across all workers
public sealed class LowStockReportJob(AppDbContext db, ILogger<LowStockReportJob> logger)
{
    public const int Threshold = 5;

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var lowStock = await db.Products
            .AsNoTracking()
            .Where(p => p.Stock < Threshold)
            .OrderBy(p => p.Stock)
            .Select(p => new { p.Id, p.Name, p.Stock })
            .ToListAsync(cancellationToken);

        foreach (var product in lowStock)
        {
            logger.LogWarning("Low stock: product {ProductId} {ProductName} has {Stock} left", product.Id, product.Name, product.Stock);
        }

        logger.LogInformation("Low stock report: {Count} product(s) below {Threshold}", lowStock.Count, Threshold);
        return lowStock.Count;
    }
}
