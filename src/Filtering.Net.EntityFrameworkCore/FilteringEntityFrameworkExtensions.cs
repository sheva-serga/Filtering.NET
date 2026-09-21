using Microsoft.EntityFrameworkCore;

namespace Filtering.Net.EntityFrameworkCore;

/// <summary>
/// EF Core async helpers that pair a <see cref="FilterRequest"/> with an
/// <see cref="IFilterDefinition{TEntity}"/> to produce a paged, materialised result.
/// </summary>
public static class FilteringEntityFrameworkExtensions
{
    /// <summary>Validates <paramref name="request"/>, applies filter/sort/paging, and returns a <see cref="PageResult{TEntity}"/> backed by a <c>COUNT</c> and <c>ToListAsync</c> against the database.</summary>
    /// <typeparam name="TEntity">The entity type the query targets.</typeparam>
    /// <param name="query">The source query. Paging is only as stable as this query's ordering: when <paramref name="request"/> pages without naming a sort, <c>SKIP</c>/<c>TAKE</c> runs with whatever <c>ORDER BY</c> the source carries, and with none at all the provider may return overlapping or missing rows across pages. Either send a sort in the request or order <paramref name="query"/> on a unique key before calling.</param>
    /// <param name="definition">The filter definition that validates the request and builds the predicate, sorting, and paging.</param>
    /// <param name="request">The request to apply.</param>
    /// <param name="cancellationToken">Cancels the <c>COUNT</c> and the materialisation.</param>
    /// <returns>The materialised slice plus the total filtered row count. <see cref="PageResult{TItem}.PageSize"/> is the page size the definition actually applied (see <see cref="IFilterDefinition{TEntity}.ResolvePageSize"/>); when the request sets neither <c>Page</c> nor <c>PageSize</c> nothing is paged and the whole filtered set is reported as a single page.</returns>
    /// <exception cref="FilterValidationException">Thrown when the request fails validation.</exception>
    public static async Task<PageResult<TEntity>> ApplyPagedAsync<TEntity>(
        this IQueryable<TEntity> query,
        IFilterDefinition<TEntity> definition,
        FilterRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = definition.Validate(request);
        if (!validationResult.IsValid)
            throw new FilterValidationException(validationResult);

        var filteredQuery = request.Where is not null
            ? definition.ApplyFilter(query, request.Where)
            : query;

        var totalCount = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var hasSort = request.Sort is { Count: > 0 };
        var hasPaging = request.Page is not null || request.PageSize is not null;
        var itemsQuery = (hasSort || hasPaging)
            ? definition.ApplySorting(filteredQuery, request.Sort, request.Page, request.PageSize)
            : filteredQuery;

        var items = await itemsQuery.ToListAsync(cancellationToken).ConfigureAwait(false);
        var resolvedPage = Math.Max(1, request.Page ?? 1);
        // The materialised count is wrong for every partial page, so ask the definition for the size
        // it applied. Unpaged requests return everything, which is one page the size of the whole set.
        var resolvedPageSize = hasPaging ? definition.ResolvePageSize(request.PageSize) : totalCount;
        return new PageResult<TEntity>(items, totalCount, resolvedPage, resolvedPageSize);
    }
}
