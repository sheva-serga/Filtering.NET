namespace Filtering.Net;

/// <summary>Marks a method as a per-property override mapping. The method body declares custom operators via a <see cref="FilterRuleBuilder{TEntity, TValue}"/>.</summary>
/// <param name="propertyName">Name of the property to override.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class PropertyMapAttribute(string propertyName) : Attribute
{
    /// <summary>The name of the property whose mapping this method overrides.</summary>
    public string PropertyName { get; } = propertyName;
}
