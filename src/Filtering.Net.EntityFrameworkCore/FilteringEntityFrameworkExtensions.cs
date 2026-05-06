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
