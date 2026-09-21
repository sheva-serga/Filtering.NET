namespace Filtering.Net;

/// <summary>The filter engine: validates requests and applies them to a query according to a <see cref="FilterSchema{TEntity}"/>. Generated filter classes derive from it; it can also be constructed directly from a hand-built schema.</summary>
/// <typeparam name="TEntity">The entity type the definition targets.</typeparam>
/// <remarks>Creates a definition over <paramref name="schema"/>.</remarks>
public class FilterDefinition<TEntity>(FilterSchema<TEntity> schema) : IFilterDefinition<TEntity>
{
    /// <summary>The properties, operators, and limits this definition accepts.</summary>
    public FilterSchema<TEntity> Schema { get; } = schema ?? throw new ArgumentNullException(nameof(schema));

    /// <inheritdoc />
    public FilterValidationResult Validate(FilterNode? where) => FilterTreeValidator.Validate(Schema, where);

    /// <inheritdoc />
    public FilterValidationResult Validate(IReadOnlyList<SortItem>? sortItems) => FilterTreeValidator.ValidateSort(Schema, sortItems);

    /// <inheritdoc />
    public FilterValidationResult Validate(int? page, int? pageSize) =>
        PageValidation.Validate(page, pageSize, ResolvePageSize(pageSize), Schema.Settings.MaxPageSize);

    /// <inheritdoc />
    public FilterValidationResult Validate(FilterRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var aggregatedErrors = new List<FilterValidationError>();
        if (request.Where is not null) aggregatedErrors.AddRange(Validate(request.Where).Errors);
        if (request.Sort is not null) aggregatedErrors.AddRange(Validate(request.Sort).Errors);
        if (request.Page is not null || request.PageSize is not null)
        {
            aggregatedErrors.AddRange(Validate(request.Page, request.PageSize).Errors);
        }
        return aggregatedErrors.Count == 0 ? FilterValidationResult.Success : new FilterValidationResult(aggregatedErrors);
    }

    /// <inheritdoc />
    public int ResolvePageSize(int? requestedPageSize) =>
        Math.Min(Math.Max(1, requestedPageSize ?? Schema.Settings.DefaultPageSize), Schema.Settings.MaxPageSize);

    /// <inheritdoc />
    public IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, FilterNode? where)
    {
        if (where is null) return query;
        return query.Where(FilterPredicateComposer.Compose(Schema, where));
    }

    /// <inheritdoc />
    public IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, IReadOnlyList<SortItem>? sortItems, int? page = null, int? pageSize = null)
    {
        var resultQuery = query;
        if (sortItems is { Count: > 0 })
        {
            if (!Schema.HasSortableProperties)
                throw new FilterDispatchException($"No sortable fields are configured (got '{sortItems[0].Field}').");

            IOrderedQueryable<TEntity>? orderedQuery = null;
            foreach (var sortItem in sortItems)
            {
                if (!Schema.TryGetProperty(sortItem.Field, out var property))
                    throw new FilterDispatchException($"Unknown sort field '{sortItem.Field}' (validation should have caught this).");
                if (!property.Sortable)
                    throw new FilterDispatchException($"Field '{sortItem.Field}' is not configured as sortable (validation should have caught this).");
                orderedQuery = property.ApplySort(query, orderedQuery, sortItem.Dir ?? property.DefaultSortDirection);
            }
            resultQuery = orderedQuery!;
        }

        if (page is not null || pageSize is not null)
        {
            var pageNumber = Math.Max(1, page ?? 1);
            var resolvedPageSize = ResolvePageSize(pageSize);
            // Computed in long so an oversized page fails loudly instead of wrapping into a negative Skip.
            var skippedRowCount = (long)(pageNumber - 1) * resolvedPageSize;
            if (skippedRowCount > int.MaxValue)
                throw new FilterDispatchException(
                    $"Page {pageNumber} at page size {resolvedPageSize} skips more rows than a query can express (validation should have caught this).");
            resultQuery = resultQuery.Skip((int)skippedRowCount).Take(resolvedPageSize);
        }
        return resultQuery;
    }
}
