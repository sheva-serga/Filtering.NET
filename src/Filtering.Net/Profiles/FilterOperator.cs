using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Extracts a typed value from a JSON element; the shape of every profile's <c>TryGetValue</c> / <c>TryGetArray</c>.</summary>
public delegate bool TryParseValue<TValue>(JsonElement element, out TValue value, out string error);

/// <summary>One named operator over columns of type <typeparamref name="TColumn"/>. Create instances through <see cref="FilterOperator"/>.</summary>
public abstract class FilterOperator<TColumn>
{
    private protected FilterOperator(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new FilterConfigurationException("A filter operator name must be a non-empty string.");
        Name = name;
    }

    /// <summary>The operator name clients send in the <c>op</c> field; matched case-insensitively.</summary>
    public string Name { get; }

    internal abstract bool RequiresSerializerOptions { get; }

    internal abstract BoundOperator Bind(OperatorBinding<TColumn> binding);
}

/// <summary>Factories for <see cref="FilterOperator{TColumn}"/>.</summary>
public static class FilterOperator
{
    /// <summary>An operator whose value is extracted by <paramref name="parser"/>, typically a profile's <c>TryGetValue</c> or <c>TryGetArray</c>.</summary>
    public static FilterOperator<TColumn> Value<TColumn, TValue>(
        string name,
        Expression<Func<TColumn, TValue, bool>> predicate,
        TryParseValue<TValue> parser)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        if (parser is null) throw new ArgumentNullException(nameof(parser));
        return new ValueOperator<TColumn, TValue>(name, predicate, parser);
    }

    /// <summary>An operator whose value is deserialized through System.Text.Json using the definition's type-info resolver.</summary>
    public static FilterOperator<TColumn> Value<TColumn, TValue>(
        string name,
        Expression<Func<TColumn, TValue, bool>> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return new ValueOperator<TColumn, TValue>(name, predicate, parser: null);
    }

    /// <summary>An operator that takes no value, declared over the column type itself.</summary>
    public static FilterOperator<TColumn> Unary<TColumn>(string name, Expression<Func<TColumn, bool>> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return new UnaryOperator<TColumn>(name, predicate);
    }

    /// <summary>An operator that takes no value, declared over the nullable form of a value-type column (for example <c>isNull</c> on <c>int?</c>).</summary>
    public static FilterOperator<TColumn> UnaryOverNullable<TColumn>(string name, Expression<Func<TColumn?, bool>> predicate)
        where TColumn : struct
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return new UnaryOperator<TColumn>(name, predicate);
    }
}
