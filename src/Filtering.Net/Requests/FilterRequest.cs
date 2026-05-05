namespace Filtering.Net;

/// <summary>Top-level request shape: filter expression, sort items, paging.</summary>
public sealed record FilterRequest
{
    /// <summary>Optional filter expression tree.</summary>
    public FilterNode? Where { get; init; }

    /// <summary>Optional ordered list of sort directives.</summary>
    public IReadOnlyList<SortItem>? Sort { get; init; }

    /// <summary>1-based page index. Null means no paging.</summary>
    public int? Page { get; init; }

    /// <summary>Page size. Null means no paging.</summary>
    public int? PageSize { get; init; }
}
