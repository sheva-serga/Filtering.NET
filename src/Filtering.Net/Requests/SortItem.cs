using System.Diagnostics.CodeAnalysis;

namespace Filtering.Net;

/// <summary>A single sort directive: which field, in which direction.</summary>
/// <param name="Field">The configured sortable field name.</param>
/// <param name="Dir">Sort direction. When omitted, the property's configured default direction applies.</param>
public sealed record SortItem(string Field, SortDir? Dir = null)
{
    /// <summary>Parses a query-string sort item: <c>field</c>, <c>field:asc</c>, or <c>field:desc</c> (suffix case-insensitive, split on the last colon).</summary>
    public static bool TryParse(string? value, IFormatProvider? provider, [NotNullWhen(true)] out SortItem? result)
    {
        result = null;
        if (value is null || string.IsNullOrWhiteSpace(value)) return false;

        var separatorIndex = value.LastIndexOf(':');
        if (separatorIndex < 0)
        {
            result = new SortItem(value);
            return true;
        }

        var fieldName = value.Substring(0, separatorIndex);
        if (string.IsNullOrWhiteSpace(fieldName)) return false;

        var directionText = value.Substring(separatorIndex + 1);
        SortDir? direction =
            string.Equals(directionText, "asc", StringComparison.OrdinalIgnoreCase) ? SortDir.Asc
            : string.Equals(directionText, "desc", StringComparison.OrdinalIgnoreCase) ? SortDir.Desc
            : null;
        if (direction is null) return false;

        result = new SortItem(fieldName, direction);
        return true;
    }
}
