namespace Filtering.Net;

/// <summary>A single sort directive: which field, in which direction.</summary>
/// <param name="Field">The configured sortable field name.</param>
/// <param name="Dir">Sort direction. When omitted, the property's configured default direction applies.</param>
public sealed record SortItem(string Field, SortDir? Dir = null);
