namespace Filtering.Net;

#if NET6_0_OR_GREATER
/// <summary>Built-in profile for <see cref="DateOnly"/> properties.</summary>
[FilterProfile<global::System.DateOnly>]
public static class DateOnlyFilter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]
    public static Expression<Func<DateOnly, DateOnly, bool>> Eq => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")]
    public static Expression<Func<DateOnly, DateOnly, bool>> Ne => (column, value) => column != value;

    /// <summary>Greater-than operator template (<c>gt</c>).</summary>
    [FilterOperator("gt")]
    public static Expression<Func<DateOnly, DateOnly, bool>> Gt => (column, value) => column > value;

    /// <summary>Greater-than-or-equal operator template (<c>gte</c>).</summary>
    [FilterOperator("gte")]
    public static Expression<Func<DateOnly, DateOnly, bool>> Gte => (column, value) => column >= value;

    /// <summary>Less-than operator template (<c>lt</c>).</summary>
    [FilterOperator("lt")]
    public static Expression<Func<DateOnly, DateOnly, bool>> Lt => (column, value) => column < value;

    /// <summary>Less-than-or-equal operator template (<c>lte</c>).</summary>
    [FilterOperator("lte")]
    public static Expression<Func<DateOnly, DateOnly, bool>> Lte => (column, value) => column <= value;

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")]
    public static Expression<Func<DateOnly?, bool>> IsNull => column => column == null;

    /// <summary>Extracts a <see cref="DateOnly"/> from an ISO 8601 date string (yyyy-MM-dd).</summary>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="value">The extracted DateOnly, or <c>default</c> on failure.</param>
    /// <param name="error">A human-readable explanation of the failure.</param>
    public static bool TryGetValue(JsonElement element, out DateOnly value, out string error)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            value = default;
            error = $"Expected JSON String for DateOnly, got {element.ValueKind}.";
            return false;
        }
        var rawString = element.GetString();
        if (rawString is not null && DateOnly.TryParse(rawString, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            error = string.Empty;
            return true;
        }
        value = default;
        error = $"String '{rawString}' is not a valid ISO 8601 DateOnly.";
        return false;
    }
}
#endif
