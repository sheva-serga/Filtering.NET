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
            if (property.RequiresSerializerOptions && serializerOptions is null)
                throw new FilterConfigurationException(
                    $"Property '{property.Field}' has an operator that takes a typed value, so the schema needs JsonSerializerOptions with a type-info resolver.");
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

    internal bool TryGetProperty(string wireKey, out FilterProperty<TEntity> property) =>
        _propertiesByWireKey.TryGetValue(wireKey, out property!);

    /// <summary>Re-roots this schema's properties on a parent entity. <paramref name="only"/> and <paramref name="except"/> name this schema's own property paths; properties it nests itself are always carried along.</summary>
    public IReadOnlyList<FilterProperty<TParent>> LiftInto<TParent>(
        Expression<Func<TParent, TEntity>> navigation,
        string prefix,
        IReadOnlyCollection<string>? only = null,
        IReadOnlyCollection<string>? except = null,
        bool disableSorting = false)
    {
        var liftedProperties = new List<FilterProperty<TParent>>(Properties.Count);
        foreach (var property in Properties)
        {
            if (!property.IsLifted && !IsPathAllowed(property.Field, only, except)) continue;
            liftedProperties.Add(property.LiftThrough(navigation, prefix, disableSorting));
        }
        return liftedProperties;
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

    private static bool IsPathAllowed(string relativePath, IReadOnlyCollection<string>? only, IReadOnlyCollection<string>? except)
    {
        if (only is { Count: > 0 } && !ContainsPath(only, relativePath)) return false;
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

    /// <summary>Builds the schema. Throws <see cref="FilterConfigurationException"/> for a duplicate wire key, or for a typed-value operator when no serializer options were given.</summary>
    public FilterSchema<TEntity> Build() => new([.. _properties], _settings, serializerOptions);
}
