#pragma warning disable IDE0130 // Namespace does not match folder structure

using System.Linq.Expressions;

namespace Filtering.Net;

/// <summary>One filterable (and optionally sortable) property of <typeparamref name="TEntity"/>. Create instances through <see cref="FilterProperty"/>.</summary>
public abstract class FilterProperty<TEntity>
{
    private protected FilterProperty(
        string field,
        string? alias,
        bool sortable,
        SortDir defaultSortDirection,
        string profileName,
        bool isLifted)
    {
        Field = field;
        Alias = alias;
        Sortable = sortable;
        DefaultSortDirection = defaultSortDirection;
        ProfileName = profileName;
        IsLifted = isLifted;
    }

    /// <summary>The CLR property path, which is also a wire key (for example <c>Department.Name</c>). Matched case-insensitively.</summary>
    public string Field { get; }

    /// <summary>An additional wire key for the same property, or null.</summary>
    public string? Alias { get; }

    /// <summary>Whether the property may appear in a request's sort list.</summary>
    public bool Sortable { get; }

    /// <summary>The direction used when a sort item names this property without a direction.</summary>
    public SortDir DefaultSortDirection { get; }

    /// <summary>The name of the profile the operators came from.</summary>
    public string ProfileName { get; }

    /// <summary>The operator names this property accepts, after <c>Only</c> / <c>Except</c>.</summary>
    public abstract IReadOnlyCollection<string> Operators { get; }

    // True for properties that arrived through a nested filter; Only / Except of an outer nesting skip them.
    internal bool IsLifted { get; }

    internal abstract bool RequiresSerializerOptions { get; }

    internal abstract void ValidateLeaf(FilterLeaf leaf, string path, List<FilterValidationError> errors, FilterValueContext valueContext);

    internal abstract Expression<Func<TEntity, bool>> BuildPredicate(FilterLeaf leaf, FilterValueContext valueContext);

    internal abstract IOrderedQueryable<TEntity> ApplySort(IQueryable<TEntity> query, IOrderedQueryable<TEntity>? orderedQuery, SortDir direction);

    /// <summary>Re-roots this property on a parent entity by composing <paramref name="navigation"/> with the accessor and prefixing the wire keys.</summary>
    public abstract FilterProperty<TParent> LiftThrough<TParent>(
        Expression<Func<TParent, TEntity>> navigation,
        string prefix,
        bool disableSorting);
}

/// <summary>Factories for <see cref="FilterProperty{TEntity}"/>.</summary>
public static class FilterProperty
{
    /// <summary>Maps a property whose type is the profile's column type.</summary>
    public static FilterPropertyBuilder<TEntity, TColumn> Map<TEntity, TColumn>(
        string field,
        Expression<Func<TEntity, TColumn>> accessor,
        FilterProfile<TColumn> profile)
    {
        if (accessor is null) throw new ArgumentNullException(nameof(accessor));
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        return new FilterPropertyBuilder<TEntity, TColumn>(
            field,
            profile.Name,
            [.. profile.Operators.Values],
            configuration => new ColumnFilterProperty<TEntity, TColumn, TColumn>(configuration, accessor, nullableSupport: null));
    }

    /// <summary>Maps a nullable value-type property onto a profile declared over the underlying type; comparisons are lifted as C# lifts them.</summary>
    public static FilterPropertyBuilder<TEntity, TColumn> MapNullable<TEntity, TColumn>(
        string field,
        Expression<Func<TEntity, TColumn?>> accessor,
        FilterProfile<TColumn> profile)
        where TColumn : struct
    {
        if (accessor is null) throw new ArgumentNullException(nameof(accessor));
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        return new FilterPropertyBuilder<TEntity, TColumn>(
            field,
            profile.Name,
            [.. profile.Operators.Values],
            configuration => new ColumnFilterProperty<TEntity, TColumn?, TColumn>(configuration, accessor, NullableColumnSupport<TColumn>.Instance));
    }

    /// <summary>Maps a <c>[PropertyMap]</c>-style rule: a custom accessor with its own operators.</summary>
    public static FilterPropertyBuilder<TEntity, TValue> MapRule<TEntity, TValue>(string field, FilterRule<TEntity, TValue> rule)
    {
        if (rule is null) throw new ArgumentNullException(nameof(rule));
        return new FilterPropertyBuilder<TEntity, TValue>(
            field,
            "PropertyMap",
            rule.Operators,
            configuration => new ColumnFilterProperty<TEntity, TValue, TValue>(configuration, rule.PropertyAccessor, nullableSupport: null));
    }
}
