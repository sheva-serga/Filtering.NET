using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for <see cref="byte"/> properties.</summary>
[FilterProfile<byte>]
public static class ByteFilter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]   public static Expression<Func<byte, byte, bool>> Eq  => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")]   public static Expression<Func<byte, byte, bool>> Ne  => (column, value) => column != value;

    /// <summary>Greater-than operator template (<c>gt</c>).</summary>
    [FilterOperator("gt")]   public static Expression<Func<byte, byte, bool>> Gt  => (column, value) => column >  value;

    /// <summary>Greater-than-or-equal operator template (<c>gte</c>).</summary>
    [FilterOperator("gte")]  public static Expression<Func<byte, byte, bool>> Gte => (column, value) => column >= value;

    /// <summary>Less-than operator template (<c>lt</c>).</summary>
    [FilterOperator("lt")]   public static Expression<Func<byte, byte, bool>> Lt  => (column, value) => column <  value;

    /// <summary>Less-than-or-equal operator template (<c>lte</c>).</summary>
    [FilterOperator("lte")]  public static Expression<Func<byte, byte, bool>> Lte => (column, value) => column <= value;

    /// <summary>Set-membership operator template (<c>in</c>).</summary>
    [FilterOperator("in")]   public static Expression<Func<byte, byte[], bool>> In => (column, values) => values.Contains(column);

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")] public static Expression<Func<byte?, bool>> IsNull => column => column == null;

    /// <summary>Extracts a <see cref="byte"/> from a JSON Number or invariant-culture JSON String via <see cref="NumericExtractor"/>.</summary>
    public static bool TryGetValue(JsonElement element, out byte value, out string error) =>
        NumericExtractor.TryGetValue(
            element,
            (JsonElement e, out byte v) => e.TryGetByte(out v),
            (string s, out byte v) => byte.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v),
            "byte",
            out value, out error);

    /// <summary>Extracts a <see cref="byte"/>[] from a JSON Array via <see cref="TryGetValue"/>.</summary>
    public static bool TryGetArray(JsonElement element, out byte[] values, out string error) =>
        NumericExtractor.TryGetArray(element, TryGetValue, out values, out error);
}
