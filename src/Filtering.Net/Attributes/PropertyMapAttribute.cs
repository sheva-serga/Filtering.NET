namespace Filtering.Net;

/// <summary>Marks a method as a per-property override mapping. The method body declares custom operators via a <see cref="FilterRuleBuilder{TEntity, TValue}"/>.</summary>
/// <param name="propertyName">The field key of the rule; it does not have to name a member of the entity.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class PropertyMapAttribute(string propertyName) : Attribute
{
    /// <summary>The field key of the rule, accepted as a wire key in requests.</summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>Optional alias used in filter requests instead of the property name.</summary>
    public string? Alias { get; init; }

    /// <summary>When true, the rule's accessor is also sortable.</summary>
    public bool Sortable { get; init; }

    /// <summary>Default sort direction when sorted without an explicit direction.</summary>
    public SortDir DefaultSortDirection { get; init; } = SortDir.Asc;
}
