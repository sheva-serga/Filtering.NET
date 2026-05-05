namespace Filtering.Net;

/// <summary>Marks a method as a value interceptor for the named property. Runs after deserialization, before validation.</summary>
/// <remarks>Initializes a new <see cref="InterceptValueAttribute"/>.</remarks>
/// <param name="propertyName">Name of the property to intercept.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class InterceptValueAttribute(string propertyName) : Attribute
{
    /// <summary>The property whose values are intercepted.</summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>When true, the interceptor receives the raw <see cref="System.Text.Json.JsonElement"/> and returns the typed value (transforming arbitrary input). When false (default), it receives the deserialized typed value.</summary>
    public bool Raw { get; init; }
}
