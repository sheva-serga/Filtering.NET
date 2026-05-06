using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for <see cref="DateTimeOffset"/> properties.</summary>
[FilterProfile<DateTimeOffset>]
public static class DateTimeOffsetFilter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]
    public static Expression<Func<DateTimeOffset, DateTimeOffset, bool>> Eq => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")]
    public static Expression<Func<DateTimeOffset, DateTimeOffset, bool>> Ne => (column, value) => column != value;

    /// <summary>Greater-than operator template (<c>gt</c>).</summary>
    [FilterOperator("gt")]
    public static Expression<Func<DateTimeOffset, DateTimeOffset, bool>> Gt => (column, value) => column > value;

    /// <summary>Greater-than-or-equal operator template (<c>gte</c>).</summary>
    [FilterOperator("gte")]
    public static Expression<Func<DateTimeOffset, DateTimeOffset, bool>> Gte => (column, value) => column >= value;

    /// <summary>Less-than operator template (<c>lt</c>).</summary>
    [FilterOperator("lt")]
    public static Expression<Func<DateTimeOffset, DateTimeOffset, bool>> Lt => (column, value) => column < value;

    /// <summary>Less-than-or-equal operator template (<c>lte</c>).</summary>
    [FilterOperator("lte")]
    public static Expression<Func<DateTimeOffset, DateTimeOffset, bool>> Lte => (column, value) => column <= value;

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")]
    public static Expression<Func<DateTimeOffset?, bool>> IsNull => column => column == null;

    /// <summary>Extracts a <see cref="DateTimeOffset"/> from an ISO 8601 JSON String.</summary>
    public static bool TryGetValue(JsonElement element, out DateTimeOffset value, out string error)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            value = default;
            error = $"Expected JSON String for DateTimeOffset, got {element.ValueKind}.";
            return false;
        }
        if (element.TryGetDateTimeOffset(out value))
        {
            error = string.Empty;
            return true;
        }
        error = $"String '{element.GetString()}' is not a valid ISO 8601 DateTimeOffset.";
        return false;
    }
}
