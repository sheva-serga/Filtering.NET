namespace Filtering.Net;

/// <summary>Per-filter page-size limits that override the assembly-wide defaults from <see cref="FilterDefaultsAttribute"/>.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PageSettingsAttribute : Attribute
{
    // int, not int?: Nullable<int> is not a legal attribute-argument type (CS0655), so an int?
    // property makes every [PageSettings(...)] usage a compile error. "Absent" is expressed by
    // omitting the named argument, which is what the generator reads.

    /// <summary>Default page size when the request does not specify one. Omit to inherit from assembly defaults.</summary>
    public int DefaultPageSize { get; init; }

    /// <summary>Maximum page size accepted. Requests exceeding this fail validation. Omit to inherit from assembly defaults.</summary>
    public int MaxPageSize { get; init; }
}
