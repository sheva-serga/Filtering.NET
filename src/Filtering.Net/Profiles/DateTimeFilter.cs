using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for <see cref="DateTime"/> properties.</summary>
[FilterProfile<DateTime>]
public static class DateTimeFilter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]
    public static Expression<Func<DateTime, DateTime, bool>> Eq => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")]
    public static Expression<Func<DateTime, DateTime, bool>> Ne => (column, value) => column != value;

    /// <summary>Greater-than operator template (<c>gt</c>).</summary>
    [FilterOperator("gt")]
    public static Expression<Func<DateTime, DateTime, bool>> Gt => (column, value) => column > value;

    /// <summary>Greater-than-or-equal operator template (<c>gte</c>).</summary>
    [FilterOperator("gte")]
    public static Expression<Func<DateTime, DateTime, bool>> Gte => (column, value) => column >= value;

    /// <summary>Less-than operator template (<c>lt</c>).</summary>
    [FilterOperator("lt")]
    public static Expression<Func<DateTime, DateTime, bool>> Lt => (column, value) => column < value;

    /// <summary>Less-than-or-equal operator template (<c>lte</c>).</summary>
    [FilterOperator("lte")]
    public static Expression<Func<DateTime, DateTime, bool>> Lte => (column, value) => column <= value;

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")]
    public static Expression<Func<DateTime?, bool>> IsNull => column => column == null;

    /// <summary>Extracts a <see cref="DateTime"/> from an ISO 8601 JSON String.</summary>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="value">The extracted DateTime, or <c>default</c> on failure.</param>
    /// <param name="error">A human-readable explanation of the failure.</param>
    public static bool TryGetValue(JsonElement element, out DateTime value, out string error)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            value = default;
            error = $"Expected JSON String for DateTime, got {element.ValueKind}.";
            return false;
        }
        if (element.TryGetDateTime(out value))
        {
            error = string.Empty;
            return true;
        }
        error = $"String '{element.GetString()}' is not a valid ISO 8601 DateTime.";
        return false;
    }
}
