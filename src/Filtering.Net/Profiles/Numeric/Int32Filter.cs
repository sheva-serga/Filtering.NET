using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for <see cref="int"/> properties.</summary>
[FilterProfile<int>]
public static class Int32Filter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]   public static Expression<Func<int, int, bool>> Eq  => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")]   public static Expression<Func<int, int, bool>> Ne  => (column, value) => column != value;

    /// <summary>Greater-than operator template (<c>gt</c>).</summary>
    [FilterOperator("gt")]   public static Expression<Func<int, int, bool>> Gt  => (column, value) => column >  value;

    /// <summary>Greater-than-or-equal operator template (<c>gte</c>).</summary>
    [FilterOperator("gte")]  public static Expression<Func<int, int, bool>> Gte => (column, value) => column >= value;

    /// <summary>Less-than operator template (<c>lt</c>).</summary>
    [FilterOperator("lt")]   public static Expression<Func<int, int, bool>> Lt  => (column, value) => column <  value;

    /// <summary>Less-than-or-equal operator template (<c>lte</c>).</summary>
    [FilterOperator("lte")]  public static Expression<Func<int, int, bool>> Lte => (column, value) => column <= value;

    /// <summary>Set-membership operator template (<c>in</c>).</summary>
    [FilterOperator("in")]   public static Expression<Func<int, int[], bool>> In => (column, values) => values.Contains(column);

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")] public static Expression<Func<int?, bool>> IsNull => column => column == null;

    /// <summary>Extracts an <see cref="int"/> from a JSON Number or invariant-culture JSON String via <see cref="NumericExtractor"/>.</summary>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="value">The extracted int, or <c>0</c> on failure.</param>
    /// <param name="error">A human-readable explanation of the failure.</param>
    public static bool TryGetValue(JsonElement element, out int value, out string error) =>
        NumericExtractor.TryGetValue(
            element,
            (JsonElement e, out int v) => e.TryGetInt32(out v),
            (string s, out int v) => int.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v),
            "int",
            out value, out error);

    /// <summary>Extracts an <see cref="int"/>[] from a JSON Array; each element parsed via <see cref="TryGetValue"/>.</summary>
    /// <param name="element">The JSON array to read.</param>
    /// <param name="values">The extracted ints, or an empty array on failure.</param>
    /// <param name="error">A human-readable explanation of the failure.</param>
    public static bool TryGetArray(JsonElement element, out int[] values, out string error) =>
        NumericExtractor.TryGetArray(element, TryGetValue, out values, out error);
}
