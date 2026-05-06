using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

/// <summary>Built-in profile for bool (and bool?) properties.</summary>
[FilterProfile<bool>]
public static class BoolFilter
{
    /// <summary>Equality operator template (<c>eq</c>).</summary>
    [FilterOperator("eq")]
    public static Expression<Func<bool, bool, bool>> Eq => (column, value) => column == value;

    /// <summary>Null-check operator template (<c>isNull</c>).</summary>
    [FilterOperator("isNull")]
    public static Expression<Func<bool?, bool>> IsNull => column => column == null;

    /// <summary>Extracts a <see cref="bool"/> from a JSON True/False value; returns false with a populated <paramref name="error"/> for any other kind.</summary>
    public static bool TryGetValue(JsonElement element, out bool value, out string error)
    {
        if (element.ValueKind == JsonValueKind.True)
        {
            value = true;
            error = string.Empty;
            return true;
        }
        if (element.ValueKind == JsonValueKind.False)
        {
            value = false;
            error = string.Empty;
            return true;
        }
        value = false;
        error = $"Expected JSON True/False, got {element.ValueKind}.";
        return false;
    }
}
