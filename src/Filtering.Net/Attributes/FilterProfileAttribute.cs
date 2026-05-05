namespace Filtering.Net;

/// <summary>Marks a static class as the filter profile for CLR type <typeparamref name="T"/>.
/// The source generator builds a profile index keyed by <typeparamref name="T"/> and resolves
/// each filterable property's profile by looking up its CLR type.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class FilterProfileAttribute<T> : Attribute
{
    /// <summary>Optional base profile whose operators are inherited. If null, this profile
    /// defines all of its operators from scratch.</summary>
    public Type? BasedOn { get; init; }
}
