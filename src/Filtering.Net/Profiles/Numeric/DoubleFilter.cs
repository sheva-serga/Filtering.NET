using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for <see cref="double"/> properties.</summary>
[FilterProfile<double>]
public static class DoubleFilter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]   public static Expression<Func<double, double, bool>> Eq  => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")]   public static Expression<Func<double, double, bool>> Ne  => (column, value) => column != value;

    /// <summary>Greater-than operator template (<c>gt</c>).</summary>
    [FilterOperator("gt")]   public static Expression<Func<double, double, bool>> Gt  => (column, value) => column >  value;

    /// <summary>Greater-than-or-equal operator template (<c>gte</c>).</summary>
    [FilterOperator("gte")]  public static Expression<Func<double, double, bool>> Gte => (column, value) => column >= value;

    /// <summary>Less-than operator template (<c>lt</c>).</summary>
    [FilterOperator("lt")]   public static Expression<Func<double, double, bool>> Lt  => (column, value) => column <  value;

    /// <summary>Less-than-or-equal operator template (<c>lte</c>).</summary>
    [FilterOperator("lte")]  public static Expression<Func<double, double, bool>> Lte => (column, value) => column <= value;

    /// <summary>Set-membership operator template (<c>in</c>).</summary>
    [FilterOperator("in")]   public static Expression<Func<double, double[], bool>> In => (column, values) => values.Contains(column);

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")] public static Expression<Func<double?, bool>> IsNull => column => column == null;

    /// <summary>Extracts a <see cref="double"/> from a JSON Number or invariant-culture JSON String via <see cref="NumericExtractor"/>.</summary>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="value">The extracted double, or <c>0</c> on failure.</param>
    /// <param name="error">A human-readable explanation of the failure.</param>
    public static bool TryGetValue(JsonElement element, out double value, out string error) =>
        NumericExtractor.TryGetValue(
            element,
            (JsonElement e, out double v) => e.TryGetDouble(out v),
            (string s, out double v) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v),
            "double",
            out value, out error);

    /// <summary>Extracts a <see cref="double"/>[] from a JSON Array; each element parsed via <see cref="TryGetValue"/>.</summary>
    /// <param name="element">The JSON array to read.</param>
    /// <param name="values">The extracted doubles, or an empty array on failure.</param>
    /// <param name="error">A human-readable explanation of the failure.</param>
    public static bool TryGetArray(JsonElement element, out double[] values, out string error) =>
        NumericExtractor.TryGetArray(element, TryGetValue, out values, out error);
}
