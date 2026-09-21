using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Page-size and request-shape limits of one filter definition.</summary>
public sealed record FilterSettings(int DefaultPageSize = 50, int MaxPageSize = 200, int MaxNestingDepth = 10, int MaxLeafConditions = 50);

/// <summary>The immutable description of what a filter definition accepts. Build it with <see cref="FilterSchemaBuilder{TEntity}"/>.</summary>
public sealed class FilterSchema<TEntity>
{
    private readonly Dictionary<string, FilterProperty<TEntity>> _propertiesByWireKey;

    internal FilterSchema(IReadOnlyList<FilterProperty<TEntity>> properties, FilterSettings settings, JsonSerializerOptions? serializerOptions)
    {
        Properties = properties;
        Settings = settings;
        SerializerOptions = serializerOptions;

        _propertiesByWireKey = new Dictionary<string, FilterProperty<TEntity>>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in properties)
        {
            if (property.TypedValueOperators.Count > 0 && serializerOptions is null)
                throw new FilterConfigurationException(
                    $"Property '{property.Field}' has an operator that takes a typed value, so the schema needs JsonSerializerOptions with a type-info resolver. "
                    + $"Operators that take a typed value: '{string.Join("', '", property.TypedValueOperators)}'.");
            AddWireKey(property.Field, property);
            if (property.Alias is not null) AddWireKey(property.Alias, property);
            HasSortableProperties |= property.Sortable;
        }
    }

    /// <summary>Every property of the schema, in declaration order.</summary>
    public IReadOnlyList<FilterProperty<TEntity>> Properties { get; }

    /// <summary>Paging defaults and request limits.</summary>
    public FilterSettings Settings { get; }

    /// <summary>Options used to deserialize typed operator values, or null when no operator needs them.</summary>
    public JsonSerializerOptions? SerializerOptions { get; }

    internal bool HasSortableProperties { get; }

    // Request payloads reach this with a missing field, so a null key resolves to nothing instead of throwing.
    internal bool TryGetProperty(string? wireKey, out FilterProperty<TEntity> property)
    {
        if (wireKey is null)
        {
            property = null!;
            return false;
        }
        return _propertiesByWireKey.TryGetValue(wireKey, out property!);
    }

    /// <summary>Re-roots this schema's properties on a parent entity. <paramref name="only"/> and <paramref name="except"/> name this schema's own property paths; properties it nests itself are always carried along. An empty <paramref name="only"/> allows no direct property. Throws <see cref="FilterConfigurationException"/> when an entry names no property of this schema.</summary>
    public IReadOnlyList<FilterProperty<TParent>> LiftInto<TParent>(
        Expression<Func<TParent, TEntity>> navigation,
        string prefix,
        IReadOnlyCollection<string>? only = null,
        IReadOnlyCollection<string>? except = null,
        bool disableSorting = false)
    {
        // Checked before any property is lifted so an empty nested schema fails the same way a populated one does.
        if (string.IsNullOrWhiteSpace(prefix))
            throw new FilterConfigurationException(
                $"Lifting the filter for '{typeof(TEntity).Name}' through a navigation requires a non-empty prefix.");
        ThrowOnUnknownDirectPaths(only, "Only", prefix);
        ThrowOnUnknownDirectPaths(except, "Except", prefix);

        var liftedProperties = new List<FilterProperty<TParent>>(Properties.Count);
        foreach (var property in Properties)
        {
            if (!property.IsLifted && !IsPathAllowed(property.Field, only, except)) continue;
            liftedProperties.Add(property.LiftThrough(navigation, prefix, disableSorting));
        }
        return liftedProperties;
    }

    // Only a dotless entry can be checked: every dotted one names a path contributed by a nested filter, and a
    // bounded nesting legitimately drops those once its MaxDepth is used up on the path being built.
    private void ThrowOnUnknownDirectPaths(IReadOnlyCollection<string>? paths, string optionName, string prefix)
    {
        if (paths is null) return;
        List<string>? unknownPaths = null;
        foreach (var path in paths)
        {
            if (path is not null && (path.IndexOf('.') >= 0 || ContainsField(path))) continue;
            (unknownPaths ??= []).Add(path is null ? "null" : $"'{path}'");
        }
        if (unknownPaths is not null)
            throw new FilterConfigurationException(
                $"{optionName} on the nesting under '{prefix}' names {string.Join(", ", unknownPaths)}, which the filter for '{typeof(TEntity).Name}' does not expose.");
    }

    private void AddWireKey(string wireKey, FilterProperty<TEntity> property)
    {
        if (_propertiesByWireKey.TryGetValue(wireKey, out var existingProperty))
        {
            if (ReferenceEquals(existingProperty, property)) return;
            throw new FilterConfigurationException(
                $"Wire key '{wireKey}' is claimed by both '{existingProperty.Field}' and '{property.Field}'.");
        }
        _propertiesByWireKey.Add(wireKey, property);
    }

    // An empty Only allows nothing, matching FilterPropertyBuilder.Only; null is what means "no whitelist".
    private static bool IsPathAllowed(string relativePath, IReadOnlyCollection<string>? only, IReadOnlyCollection<string>? except)
    {
        if (only is not null && !ContainsPath(only, relativePath)) return false;
        return except is null || !ContainsPath(except, relativePath);
    }

    private static bool ContainsPath(IReadOnlyCollection<string> paths, string relativePath)
    {
        foreach (var path in paths)
        {
            if (string.Equals(path, relativePath, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private bool ContainsField(string relativePath)
    {
        foreach (var property in Properties)
        {
            if (string.Equals(property.Field, relativePath, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}

/// <summary>Collects properties into a <see cref="FilterSchema{TEntity}"/>.</summary>
public sealed class FilterSchemaBuilder<TEntity>(FilterSettings settings, JsonSerializerOptions? serializerOptions = null)
{
    private readonly FilterSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly List<FilterProperty<TEntity>> _properties = [];

    /// <summary>Adds one property.</summary>
    public FilterSchemaBuilder<TEntity> Add(FilterProperty<TEntity> property)
    {
        _properties.Add(property ?? throw new ArgumentNullException(nameof(property)));
        return this;
    }

    /// <summary>Adds several properties, typically the result of <see cref="FilterSchema{TNested}.LiftInto"/>.</summary>
    public FilterSchemaBuilder<TEntity> AddRange(IEnumerable<FilterProperty<TEntity>> properties)
    {
        if (properties is null) throw new ArgumentNullException(nameof(properties));
        foreach (var property in properties)
        {
            Add(property);
        }
        return this;
    }

    /// <summary>Adds another filter's properties under a navigation. <paramref name="maxDepth"/> zero means unbounded; a positive value lets the nesting identified by <paramref name="nestingKey"/> be followed that many times along one path, which is what makes circular filter graphs finite.</summary>
    public FilterSchemaBuilder<TEntity> AddNested<TNested>(
        FilterNestingContext nestingContext,
        string nestingKey,
        int maxDepth,
        Func<FilterNestingContext, FilterSchema<TNested>> nestedSchemaFactory,
        Expression<Func<TEntity, TNested>> navigation,
        string prefix,
        IReadOnlyCollection<string>? only = null,
        IReadOnlyCollection<string>? except = null,
        bool disableSorting = false)
    {
        if (nestingContext is null) throw new ArgumentNullException(nameof(nestingContext));
        if (nestedSchemaFactory is null) throw new ArgumentNullException(nameof(nestedSchemaFactory));

        if (!nestingContext.TryEnter(nestingKey, maxDepth, out var nestedContext)) return this;
        return AddRange(nestedSchemaFactory(nestedContext).LiftInto(navigation, prefix, only, except, disableSorting));
    }

    /// <summary>Builds the schema. Throws <see cref="FilterConfigurationException"/> for a duplicate wire key, or for a typed-value operator when no serializer options were given.</summary>
    public FilterSchema<TEntity> Build() => new([.. _properties], _settings, serializerOptions);
}
