namespace Filtering.Net;

/// <summary>Assembly-wide default settings picked up by every filter class (overridable per filter via <see cref="PageSettingsAttribute"/>).</summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class FilterDefaultsAttribute : Attribute
{
    /// <summary>Default page size when the request does not specify one.</summary>
    public int DefaultPageSize { get; init; } = 50;

    /// <summary>Maximum page size accepted. Requests exceeding this fail validation.</summary>
    public int MaxPageSize { get; init; } = 200;

    /// <summary>Maximum filter group nesting depth before validation rejects the request.</summary>
    public int MaxNestingDepth { get; init; } = 10;

    /// <summary>Maximum number of leaf conditions in a filter request before validation rejects it.</summary>
    public int MaxLeafConditions { get; init; } = 50;
}
