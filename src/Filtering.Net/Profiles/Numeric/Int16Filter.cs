using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for <see cref="short"/> properties.</summary>
[FilterProfile<short>]
public static class Int16Filter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")] public static Expression<Func<short, short, bool>> Eq => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")] public static Expression<Func<short, short, bool>> Ne => (column, value) => column != value;

    /// <summary>Greater-than operator template (<c>gt</c>).</summary>
    [FilterOperator("gt")] public static Expression<Func<short, short, bool>> Gt => (column, value) => column > value;

    /// <summary>Greater-than-or-equal operator template (<c>gte</c>).</summary>
    [FilterOperator("gte")] public static Expression<Func<short, short, bool>> Gte => (column, value) => column >= value;

    /// <summary>Less-than operator template (<c>lt</c>).</summary>
    [FilterOperator("lt")] public static Expression<Func<short, short, bool>> Lt => (column, value) => column < value;

    /// <summary>Less-than-or-equal operator template (<c>lte</c>).</summary>
    [FilterOperator("lte")] public static Expression<Func<short, short, bool>> Lte => (column, value) => column <= value;

    /// <summary>Set-membership operator template (<c>in</c>).</summary>
    [FilterOperator("in")] public static Expression<Func<short, short[], bool>> In => (column, values) => values.Contains(column);

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")] public static Expression<Func<short?, bool>> IsNull => column => column == null;

    /// <summary>The runtime form of this profile, built from the operator templates above.</summary>
    public static FilterProfile<short> Profile { get; } = FilterProfile<short>.Create("Int16Filter",
        FilterOperator.Value<short, short>("eq", Eq, TryGetValue),
        FilterOperator.Value<short, short>("ne", Ne, TryGetValue),
        FilterOperator.Value<short, short>("gt", Gt, TryGetValue),
        FilterOperator.Value<short, short>("gte", Gte, TryGetValue),
        FilterOperator.Value<short, short>("lt", Lt, TryGetValue),
        FilterOperator.Value<short, short>("lte", Lte, TryGetValue),
        FilterOperator.Value<short, short[]>("in", In, TryGetArray),
        FilterOperator.UnaryOverNullable<short>("isNull", IsNull));

    /// <summary>Extracts a <see cref="short"/> from a JSON Number or invariant-culture JSON String via <see cref="NumericExtractor"/>.</summary>
    public static bool TryGetValue(JsonElement element, out short value, out string error) =>
        NumericExtractor.TryGetValue(
            element,
            (JsonElement e, out short v) => e.TryGetInt16(out v),
            (string s, out short v) => short.TryParse(s, NumericExtractor.InvariantNumberStyles, CultureInfo.InvariantCulture, out v),
            "short",
            out value, out error);

    /// <summary>Extracts a <see cref="short"/>[] from a JSON Array via <see cref="TryGetValue"/>.</summary>
    public static bool TryGetArray(JsonElement element, out short[] values, out string error) =>
        NumericExtractor.TryGetArray(element, TryGetValue, out values, out error);
}
