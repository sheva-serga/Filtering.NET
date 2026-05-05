namespace Filtering.Net;

/// <summary>Marks a partial method as the configuration for one filterable property on the target entity.</summary>
/// <remarks>Initializes a new <see cref="MapAttribute"/>.</remarks>
/// <param name="propertyName">Name (or dotted nav path) of the property to map.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MapAttribute(string propertyName) : Attribute
{
    /// <summary>The name (or dotted navigation path) of the property to map. Use <c>nameof(Entity.Property)</c>.</summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>Optional explicit profile type. When null, the source generator infers the profile from the property's CLR type.</summary>
    public Type? Profile { get; init; }

    /// <summary>Optional whitelist of operator names allowed on this property (subset of the resolved profile's operators).</summary>
    public string[]? Only { get; init; }

    /// <summary>Optional blacklist of operator names excluded from the resolved profile's operators.</summary>
    public string[]? Except { get; init; }

    /// <summary>Optional alias used in filter requests instead of the property name.</summary>
    public string? Alias { get; init; }

    /// <summary>When true, the property is also sortable.</summary>
    public bool Sortable { get; init; }

    /// <summary>Default sort direction when sorted without an explicit direction.</summary>
    public SortDir DefaultSortDirection { get; init; } = SortDir.Asc;
}
