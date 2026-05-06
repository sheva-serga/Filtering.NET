namespace Filtering.Net;

/// <summary>Top-level entry point that ties an <see cref="IFilterDefinition{TEntity}"/> to an <see cref="IQueryable{TEntity}"/>.</summary>
public static class FilteringQueryableExtensions
{
    /// <summary>Validates the request, then applies filter, sorting, and paging in order. Throws <see cref="FilterValidationException"/> on invalid input.</summary>
    public static IQueryable<TEntity> Apply<TEntity>(
        this IQueryable<TEntity> query,
        IFilterDefinition<TEntity> definition,
        FilterRequest request)
    {
        var validation = definition.Validate(request);
        if (!validation.IsValid)
            throw new FilterValidationException(validation);

        var filteredQuery = request.Where is not null
            ? definition.ApplyFilter(query, request.Where)
            : query;

        var hasSort = request.Sort is { Count: > 0 };
        var hasPaging = request.Page is not null || request.PageSize is not null;

        return (hasSort || hasPaging)
            ? definition.ApplySorting(filteredQuery, request.Sort, request.Page, request.PageSize)
            : filteredQuery;
    }
}
