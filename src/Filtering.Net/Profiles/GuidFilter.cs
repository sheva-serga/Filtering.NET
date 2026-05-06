using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for Guid (and Guid?) properties.</summary>
[FilterProfile<Guid>]
public static class GuidFilter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]
    public static Expression<Func<Guid, Guid, bool>> Eq => (column, value) => column == value;

    /// <summary>Inequality operator template (<c>ne</c>).</summary>
    [FilterOperator("ne")]
    public static Expression<Func<Guid, Guid, bool>> Ne => (column, value) => column != value;

    /// <summary>Set-membership operator template (<c>in</c>).</summary>
    [FilterOperator("in")]
    public static Expression<Func<Guid, Guid[], bool>> In => (column, values) => values.Contains(column);

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")]
    public static Expression<Func<Guid?, bool>> IsNull => column => column == null;

    /// <summary>Extracts a <see cref="Guid"/> from a JSON String; returns false for non-string values or invalid GUID formats.</summary>
    public static bool TryGetValue(JsonElement element, out Guid value, out string error)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            value = Guid.Empty;
            error = $"Expected JSON String for Guid, got {element.ValueKind}.";
            return false;
        }
        if (element.TryGetGuid(out value))
        {
            error = string.Empty;
            return true;
        }
        error = $"String '{element.GetString()}' is not a valid Guid.";
        return false;
    }

    /// <summary>Extracts a <see cref="Guid"/>[] from a JSON Array of Guid strings via <see cref="TryGetValue"/>.</summary>
    public static bool TryGetArray(JsonElement element, out Guid[] values, out string error)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            values = [];
            error = $"Expected JSON Array of Guids, got {element.ValueKind}.";
            return false;
        }
        var collected = new List<Guid>();
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
