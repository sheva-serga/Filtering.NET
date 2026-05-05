namespace Filtering.Net;

/// <summary>Generated implementation per [GenerateFilter&lt;TEntity&gt;] partial class.</summary>
/// <typeparam name="TEntity">The entity type the filter targets.</typeparam>
public interface IFilterDefinition<TEntity>
{
    /// <summary>Validates a single filter expression tree.</summary>
    FilterValidationResult Validate(FilterNode? where);

    /// <summary>Validates a list of sort directives.</summary>
    FilterValidationResult Validate(IReadOnlyList<SortItem>? sortItems);

    /// <summary>Validates paging parameters.</summary>
    FilterValidationResult Validate(int? page, int? pageSize);

    /// <summary>Validates the full request, aggregating errors from all sub-validations.</summary>
    FilterValidationResult Validate(FilterRequest request);

    /// <summary>Applies the validated filter expression to the query.</summary>
    IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, FilterNode? where);

    /// <summary>Applies sorting and (optionally) paging to the query.</summary>
    IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, IReadOnlyList<SortItem>? sortItems, int? page = null, int? pageSize = null);
}
