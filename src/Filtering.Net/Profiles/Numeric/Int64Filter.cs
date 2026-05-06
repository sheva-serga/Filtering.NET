using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for <see cref="long"/> properties.</summary>
[FilterProfile<long>]
public static class Int64Filter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]   public static Expression<Func<long, long, bool>> Eq  => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")]   public static Expression<Func<long, long, bool>> Ne  => (column, value) => column != value;

    /// <summary>Greater-than operator template (<c>gt</c>).</summary>
    [FilterOperator("gt")]   public static Expression<Func<long, long, bool>> Gt  => (column, value) => column >  value;

    /// <summary>Greater-than-or-equal operator template (<c>gte</c>).</summary>
    [FilterOperator("gte")]  public static Expression<Func<long, long, bool>> Gte => (column, value) => column >= value;

    /// <summary>Less-than operator template (<c>lt</c>).</summary>
    [FilterOperator("lt")]   public static Expression<Func<long, long, bool>> Lt  => (column, value) => column <  value;

    /// <summary>Less-than-or-equal operator template (<c>lte</c>).</summary>
    [FilterOperator("lte")]  public static Expression<Func<long, long, bool>> Lte => (column, value) => column <= value;

    /// <summary>Set-membership operator template (<c>in</c>).</summary>
    [FilterOperator("in")]   public static Expression<Func<long, long[], bool>> In => (column, values) => values.Contains(column);

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")] public static Expression<Func<long?, bool>> IsNull => column => column == null;

    /// <summary>Extracts a <see cref="long"/> from a JSON Number or invariant-culture JSON String via <see cref="NumericExtractor"/>.</summary>
    public static bool TryGetValue(JsonElement element, out long value, out string error) =>
        NumericExtractor.TryGetValue(
            element,
            (JsonElement e, out long v) => e.TryGetInt64(out v),
            (string s, out long v) => long.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v),
            "long",
            out value, out error);

    /// <summary>Extracts a <see cref="long"/>[] from a JSON Array via <see cref="TryGetValue"/>.</summary>
    public static bool TryGetArray(JsonElement element, out long[] values, out string error) =>
        NumericExtractor.TryGetArray(element, TryGetValue, out values, out error);
}
