using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for string properties. Operators: eq, ne, contains, startsWith, endsWith, in, isNull.</summary>
[FilterProfile<string>]
public static class StringFilter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]
    public static Expression<Func<string, string, bool>> Eq => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")]
    public static Expression<Func<string, string, bool>> Ne => (column, value) => column != value;

    /// <summary>Substring containment operator template (<c>contains</c>).</summary>
    [FilterOperator("contains")]
    public static Expression<Func<string, string, bool>> Contains => (column, value) => column.Contains(value);

    /// <summary>Prefix-match operator template (<c>startsWith</c>).</summary>
    [FilterOperator("startsWith")]
    public static Expression<Func<string, string, bool>> StartsWith => (column, value) => column.StartsWith(value);

    /// <summary>Suffix-match operator template (<c>endsWith</c>).</summary>
    [FilterOperator("endsWith")]
    public static Expression<Func<string, string, bool>> EndsWith => (column, value) => column.EndsWith(value);

    /// <summary>Set-membership operator template (<c>in</c>).</summary>
    [FilterOperator("in")]
    public static Expression<Func<string, string[], bool>> In => (column, values) => values.Contains(column);

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")]
    public static Expression<Func<string, bool>> IsNull => column => column == null;

    /// <summary>Extracts a <see cref="string"/> from a JSON String value. Returns
    /// <see langword="false"/> with a populated <paramref name="error"/> when the JSON value
    /// is any other kind.</summary>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="value">The extracted string, or <see cref="string.Empty"/> on failure.</param>
    /// <param name="error">A human-readable explanation of the type mismatch on failure.</param>
    public static bool TryGetValue(JsonElement element, out string value, out string error)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString() ?? string.Empty;
            error = string.Empty;
            return true;
        }
        value = string.Empty;
        error = $"Expected JSON String, got {element.ValueKind}.";
        return false;
    }

    /// <summary>Extracts a <see cref="string"/>[] from a JSON Array of Strings. Returns
    /// <see langword="false"/> with a populated <paramref name="error"/> when the JSON value
    /// is not an array or any element is not a JSON String.</summary>
    /// <param name="element">The JSON array to read.</param>
    /// <param name="values">The extracted strings, or an empty array on failure.</param>
    /// <param name="error">A human-readable explanation of the type mismatch on failure.</param>
    public static bool TryGetArray(JsonElement element, out string[] values, out string error)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            values = [];
            error = $"Expected JSON Array, got {element.ValueKind}.";
            return false;
        }
        var collected = new List<string>();
        var elementIndex = 0;
        foreach (var item in element.EnumerateArray())
        {
            if (!TryGetValue(item, out var itemValue, out var itemError))
            {
                values = [];
                error = $"Array element [{elementIndex}]: {itemError}";
                return false;
            }
            collected.Add(itemValue);
            elementIndex++;
        }
        values = [.. collected];
        error = string.Empty;
        return true;
    }
}
