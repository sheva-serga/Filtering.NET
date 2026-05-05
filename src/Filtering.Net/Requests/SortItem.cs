namespace Filtering.Net;

/// <summary>A single sort directive: which field, in which direction.</summary>
/// <param name="Field">The configured sortable field name.</param>
/// <param name="Dir">Sort direction. Defaults to <see cref="SortDir.Asc"/>.</param>
public sealed record SortItem(string Field, SortDir Dir = SortDir.Asc);
