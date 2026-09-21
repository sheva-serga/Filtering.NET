using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for <see cref="double"/> properties.</summary>
[FilterProfile<double>]
public static class DoubleFilter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")] public static Expression<Func<double, double, bool>> Eq => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")] public static Expression<Func<double, double, bool>> Ne => (column, value) => column != value;

    /// <summary>Greater-than operator template (<c>gt</c>).</summary>
    [FilterOperator("gt")] public static Expression<Func<double, double, bool>> Gt => (column, value) => column > value;

    /// <summary>Greater-than-or-equal operator template (<c>gte</c>).</summary>
    [FilterOperator("gte")] public static Expression<Func<double, double, bool>> Gte => (column, value) => column >= value;

    /// <summary>Less-than operator template (<c>lt</c>).</summary>
    [FilterOperator("lt")] public static Expression<Func<double, double, bool>> Lt => (column, value) => column < value;

    /// <summary>Less-than-or-equal operator template (<c>lte</c>).</summary>
    [FilterOperator("lte")] public static Expression<Func<double, double, bool>> Lte => (column, value) => column <= value;

    /// <summary>Set-membership operator template (<c>in</c>).</summary>
    [FilterOperator("in")] public static Expression<Func<double, double[], bool>> In => (column, values) => values.Contains(column);

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")] public static Expression<Func<double?, bool>> IsNull => column => column == null;

    /// <summary>The runtime form of this profile, built from the operator templates above.</summary>
    public static FilterProfile<double> Profile { get; } = FilterProfile<double>.Create("DoubleFilter",
        FilterOperator.Value<double, double>("eq", Eq, TryGetValue),
        FilterOperator.Value<double, double>("ne", Ne, TryGetValue),
        FilterOperator.Value<double, double>("gt", Gt, TryGetValue),
        FilterOperator.Value<double, double>("gte", Gte, TryGetValue),
        FilterOperator.Value<double, double>("lt", Lt, TryGetValue),
        FilterOperator.Value<double, double>("lte", Lte, TryGetValue),
        FilterOperator.Value<double, double[]>("in", In, TryGetArray),
        FilterOperator.UnaryOverNullable<double>("isNull", IsNull));

    /// <summary>Extracts a <see cref="double"/> from a JSON Number or invariant-culture JSON String via <see cref="NumericExtractor"/>.</summary>
    public static bool TryGetValue(JsonElement element, out double value, out string error) =>
        NumericExtractor.TryGetValue(
            element,
            (JsonElement e, out double v) => e.TryGetDouble(out v),
            (string s, out double v) => double.TryParse(s, NumericExtractor.FloatingPointNumberStyles, CultureInfo.InvariantCulture, out v),
            "double",
            out value, out error);

    /// <summary>Extracts a <see cref="double"/>[] from a JSON Array via <see cref="TryGetValue"/>.</summary>
    public static bool TryGetArray(JsonElement element, out double[] values, out string error) =>
        NumericExtractor.TryGetArray(element, TryGetValue, out values, out error);
}
