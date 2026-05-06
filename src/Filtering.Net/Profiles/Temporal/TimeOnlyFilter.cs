namespace Filtering.Net;

#if NET6_0_OR_GREATER
/// <summary>Built-in profile for <see cref="TimeOnly"/> properties.</summary>
[FilterProfile<global::System.TimeOnly>]
public static class TimeOnlyFilter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]
    public static Expression<Func<TimeOnly, TimeOnly, bool>> Eq => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")]
    public static Expression<Func<TimeOnly, TimeOnly, bool>> Ne => (column, value) => column != value;

    /// <summary>Greater-than operator template (<c>gt</c>).</summary>
    [FilterOperator("gt")]
    public static Expression<Func<TimeOnly, TimeOnly, bool>> Gt => (column, value) => column > value;

    /// <summary>Greater-than-or-equal operator template (<c>gte</c>).</summary>
    [FilterOperator("gte")]
    public static Expression<Func<TimeOnly, TimeOnly, bool>> Gte => (column, value) => column >= value;

    /// <summary>Less-than operator template (<c>lt</c>).</summary>
    [FilterOperator("lt")]
    public static Expression<Func<TimeOnly, TimeOnly, bool>> Lt => (column, value) => column < value;

    /// <summary>Less-than-or-equal operator template (<c>lte</c>).</summary>
    [FilterOperator("lte")]
    public static Expression<Func<TimeOnly, TimeOnly, bool>> Lte => (column, value) => column <= value;

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")]
    public static Expression<Func<TimeOnly?, bool>> IsNull => column => column == null;

    /// <summary>Extracts a <see cref="TimeOnly"/> from an ISO 8601 time JSON String (HH:mm:ss).</summary>
    public static bool TryGetValue(JsonElement element, out TimeOnly value, out string error)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            value = default;
            error = $"Expected JSON String for TimeOnly, got {element.ValueKind}.";
            return false;
        }
        var rawString = element.GetString();
        if (rawString is not null && TimeOnly.TryParse(rawString, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            error = string.Empty;
            return true;
        }
        value = default;
        error = $"String '{rawString}' is not a valid ISO 8601 TimeOnly.";
        return false;
    }
}
#endif
