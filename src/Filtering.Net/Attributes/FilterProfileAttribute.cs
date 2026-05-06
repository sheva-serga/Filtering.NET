namespace Filtering.Net;

/// <summary>Marks a static class as the built-in or custom filter profile for CLR type <typeparamref name="T"/>.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class FilterProfileAttribute<T> : Attribute
{
    /// <summary>Optional base profile to inherit operators from; null means this profile defines all operators itself.</summary>
    public Type? BasedOn { get; init; }
}
