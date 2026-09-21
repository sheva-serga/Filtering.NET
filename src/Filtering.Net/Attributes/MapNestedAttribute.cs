namespace Filtering.Net;

/// <summary>Inlines another <c>[GenerateFilter&lt;TNav&gt;]</c> partial's mappings into this filter under a dotted prefix.</summary>
/// <param name="navigationPropertyName">Name of the navigation property to inline. Use <c>nameof(Entity.Navigation)</c>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MapNestedAttribute(string navigationPropertyName) : Attribute
{
    /// <summary>Name of the navigation property whose target filter is inlined.</summary>
    public string NavigationPropertyName { get; } = navigationPropertyName;

    /// <summary>Optional dotted prefix in filter requests; defaults to the navigation property name.</summary>
    public string? Prefix { get; init; }

    /// <summary>Optional whitelist of nested paths to include, relative to the inlined filter.</summary>
    public string[]? Only { get; init; }

    /// <summary>Optional blacklist of nested paths to exclude, relative to the inlined filter.</summary>
    public string[]? Except { get; init; }

    /// <summary>When true, every inlined mapping is demoted to filter-only.</summary>
    public bool DisableSorting { get; init; }

    /// <summary>How many times this nesting may be followed along one path. Zero (the default) means unbounded. A cycle is legal as long as at least one <c>[MapNested]</c> in it declares a positive MaxDepth; an unbounded cycle is FN0014 at compile time and <c>FilterConfigurationException</c> at schema construction.</summary>
    public int MaxDepth { get; init; }
}

/// <summary>Generic-arity overload of <see cref="MapNestedAttribute"/> that pins the inlined filter class explicitly. Use to disambiguate when multiple filter classes target the same navigation entity.</summary>
/// <typeparam name="TFilter">The concrete filter class (decorated with <c>[GenerateFilter&lt;TEntity&gt;]</c>) whose mappings are inlined.</typeparam>
/// <param name="navigationPropertyName">Name of the navigation property to inline. Use <c>nameof(Entity.Navigation)</c>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MapNestedAttribute<TFilter>(string navigationPropertyName) : Attribute
{
    /// <inheritdoc cref="MapNestedAttribute.NavigationPropertyName"/>
    public string NavigationPropertyName { get; } = navigationPropertyName;

    /// <inheritdoc cref="MapNestedAttribute.Prefix"/>
    public string? Prefix { get; init; }

    /// <inheritdoc cref="MapNestedAttribute.Only"/>
    public string[]? Only { get; init; }

    /// <inheritdoc cref="MapNestedAttribute.Except"/>
    public string[]? Except { get; init; }

    /// <inheritdoc cref="MapNestedAttribute.DisableSorting"/>
    public bool DisableSorting { get; init; }

    /// <inheritdoc cref="MapNestedAttribute.MaxDepth"/>
    public int MaxDepth { get; init; }
}
