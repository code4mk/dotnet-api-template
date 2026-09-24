using Microsoft.EntityFrameworkCore;

namespace DotnetApiTemplate.Api.Common.Pagination;

public static class QueryableExtensions
{
    /// <summary>Counts and pages a query in the database. Order the query before calling this.</summary>
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var (currentPage, size) = Paging.Normalize(page, pageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResponse<T>(items, currentPage, size, totalCount);
    }
}
