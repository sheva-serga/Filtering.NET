namespace Filtering.Net;

/// <summary>Per-filter page-size limits that override the assembly-wide defaults from <see cref="FilterDefaultsAttribute"/>.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PageSettingsAttribute : Attribute
{
    /// <summary>Default page size when the request does not specify one. Null inherits from assembly defaults.</summary>
    public int? DefaultPageSize { get; init; }

    /// <summary>Maximum page size accepted. Requests exceeding this fail validation. Null inherits from assembly defaults.</summary>
    public int? MaxPageSize { get; init; }
}
