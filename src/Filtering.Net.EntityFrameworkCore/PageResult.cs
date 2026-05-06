namespace Filtering.Net.EntityFrameworkCore;

/// <summary>Paged query result: the materialised item slice, total filtered row count, and resolved page coordinates.</summary>
/// <typeparam name="TItem">The element type of the materialised page.</typeparam>
public sealed record PageResult<TItem>(
    IReadOnlyList<TItem> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>Total pages derived from <see cref="TotalCount"/> and <see cref="PageSize"/>; at least 1.</summary>
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>True when there is at least one page before the current one.</summary>
    public bool HasPrevious => Page > 1;

    /// <summary>True when there is at least one page after the current one.</summary>
    public bool HasNext => Page < TotalPages;
}
