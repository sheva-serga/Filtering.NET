namespace Filtering.Net;

/// <summary>Query-string binding shape for a <see cref="FilterRequest"/>: <c>where</c> (JSON tree), repeated <c>sort</c> items, <c>page</c>, <c>pageSize</c>. Bind with <c>[AsParameters]</c> or <c>[FromQuery]</c>, then call <see cref="ToRequest"/>.</summary>
public sealed record FilterQuery
{
    /// <summary>The filter tree parsed from the <c>where</c> parameter.</summary>
    public FilterNode? Where { get; init; }

    /// <summary>One item per repeated <c>sort</c> parameter.</summary>
    public SortItem[]? Sort { get; init; }

    /// <summary>1-based page index.</summary>
    public int? Page { get; init; }

    /// <summary>Page size.</summary>
    public int? PageSize { get; init; }

    /// <summary>Converts the bound parameters into the request the filter engine consumes.</summary>
    public FilterRequest ToRequest() =>
        // Minimal APIs bind an absent repeated parameter as an empty array; MVC binds null. Both mean "no sort".
        new() { Where = Where, Sort = Sort is { Length: > 0 } ? Sort : null, Page = Page, PageSize = PageSize };
}
