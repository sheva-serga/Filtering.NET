using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for <see cref="float"/> properties.</summary>
[FilterProfile<float>]
public static class SingleFilter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]   public static Expression<Func<float, float, bool>> Eq  => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")]   public static Expression<Func<float, float, bool>> Ne  => (column, value) => column != value;

    /// <summary>Greater-than operator template (<c>gt</c>).</summary>
    [FilterOperator("gt")]   public static Expression<Func<float, float, bool>> Gt  => (column, value) => column >  value;

    /// <summary>Greater-than-or-equal operator template (<c>gte</c>).</summary>
    [FilterOperator("gte")]  public static Expression<Func<float, float, bool>> Gte => (column, value) => column >= value;

    /// <summary>Less-than operator template (<c>lt</c>).</summary>
    [FilterOperator("lt")]   public static Expression<Func<float, float, bool>> Lt  => (column, value) => column <  value;

    /// <summary>Less-than-or-equal operator template (<c>lte</c>).</summary>
    [FilterOperator("lte")]  public static Expression<Func<float, float, bool>> Lte => (column, value) => column <= value;

    /// <summary>Set-membership operator template (<c>in</c>).</summary>
    [FilterOperator("in")]   public static Expression<Func<float, float[], bool>> In => (column, values) => values.Contains(column);

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")] public static Expression<Func<float?, bool>> IsNull => column => column == null;

    /// <summary>The runtime form of this profile, built from the operator templates above.</summary>
    public static FilterProfile<float> Profile { get; } = FilterProfile<float>.Create("SingleFilter",
        FilterOperator.Value<float, float>("eq", Eq, TryGetValue),
        FilterOperator.Value<float, float>("ne", Ne, TryGetValue),
        FilterOperator.Value<float, float>("gt", Gt, TryGetValue),
        FilterOperator.Value<float, float>("gte", Gte, TryGetValue),
        FilterOperator.Value<float, float>("lt", Lt, TryGetValue),
        FilterOperator.Value<float, float>("lte", Lte, TryGetValue),
        FilterOperator.Value<float, float[]>("in", In, TryGetArray),
        FilterOperator.UnaryOverNullable<float>("isNull", IsNull));

    /// <summary>Extracts a <see cref="float"/> from a JSON Number or invariant-culture JSON String via <see cref="NumericExtractor"/>.</summary>
    public static bool TryGetValue(JsonElement element, out float value, out string error) =>
        NumericExtractor.TryGetValue(
            element,
            (JsonElement e, out float v) => e.TryGetSingle(out v),
            (string s, out float v) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v),
            "float",
            out value, out error);

    /// <summary>Extracts a <see cref="float"/>[] from a JSON Array via <see cref="TryGetValue"/>.</summary>
    public static bool TryGetArray(JsonElement element, out float[] values, out string error) =>
        NumericExtractor.TryGetArray(element, TryGetValue, out values, out error);
}
