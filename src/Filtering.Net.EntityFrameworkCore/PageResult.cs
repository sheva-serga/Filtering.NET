namespace Filtering.Net.EntityFrameworkCore;

/// <summary>
/// Immutable result of a paged query: the materialised slice of items, the unfiltered total
/// row count for the underlying filter, and the resolved 1-based page index and page size.
/// Computed convenience members (<see cref="TotalPages"/>, <see cref="HasPrevious"/>,
/// <see cref="HasNext"/>) make pagination metadata trivial to surface to clients.
/// </summary>
/// <typeparam name="TItem">The element type of the materialised page.</typeparam>
/// <param name="Items">The materialised items for this page in their requested order.</param>
/// <param name="TotalCount">The total number of rows that matched the filter, across all pages.</param>
/// <param name="Page">The 1-based page index returned. Always at least 1, even if the caller
/// passed a smaller value or null.</param>
/// <param name="PageSize">The page size that was actually applied. When the caller did not
/// request paging this falls back to the materialised count.</param>
public sealed record PageResult<TItem>(
    IReadOnlyList<TItem> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>The total number of pages, computed from <see cref="TotalCount"/> and
    /// <see cref="PageSize"/>. Returns 1 when <see cref="PageSize"/> is non-positive to keep
    /// arithmetic well-defined.</summary>
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>True when there is at least one page before the current one.</summary>
    public bool HasPrevious => Page > 1;

    /// <summary>True when there is at least one page after the current one.</summary>
    public bool HasNext => Page < TotalPages;
}
