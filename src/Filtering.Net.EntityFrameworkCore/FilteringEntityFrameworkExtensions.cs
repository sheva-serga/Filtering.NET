using Microsoft.EntityFrameworkCore;

namespace Filtering.Net.EntityFrameworkCore;

/// <summary>
/// EF Core async helpers that pair a <see cref="FilterRequest"/> with an
/// <see cref="IFilterDefinition{TEntity}"/> to produce a paged, materialised result.
/// </summary>
public static class FilteringEntityFrameworkExtensions
{
    /// <summary>
    /// Validates the request, applies the filter expression and sort/paging, then asynchronously
    /// executes both a <c>COUNT</c> over the filtered query and a <c>ToListAsync</c> over the
    /// paginated query against the database. Returns a <see cref="PageResult{TEntity}"/> bundling
    /// the slice with the unfiltered total count.
    /// </summary>
    /// <typeparam name="TEntity">The entity type the query targets.</typeparam>
    /// <param name="query">The base <see cref="IQueryable{T}"/> (typically a DbSet or a
    /// pre-narrowed projection of one).</param>
    /// <param name="definition">The generated filter definition for <typeparamref name="TEntity"/>.</param>
    /// <param name="request">The structured filter/sort/page request to apply.</param>
    /// <param name="cancellationToken">Token forwarded to the async EF Core operations.</param>
    /// <exception cref="FilterValidationException">Thrown when the request fails validation.</exception>
    public static async Task<PageResult<TEntity>> ApplyPagedAsync<TEntity>(
        this IQueryable<TEntity> query,
        IFilterDefinition<TEntity> definition,
        FilterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        if (request is null) throw new ArgumentNullException(nameof(request));

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
        var resolvedPageSize = request.PageSize ?? items.Count;
        return new PageResult<TEntity>(items, totalCount, resolvedPage, resolvedPageSize);
    }
}
