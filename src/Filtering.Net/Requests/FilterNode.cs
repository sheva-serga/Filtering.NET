using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Filtering.Net;

/// <summary>Base type for filter expressions. Concrete types: <see cref="FilterGroup"/>, <see cref="FilterLeaf"/>.</summary>
[JsonConverter(typeof(FilterNodeJsonConverter))]
public abstract record FilterNode
{
    /// <summary>Parses a filter tree from its JSON text, as sent in a query-string <c>where</c> parameter; returns <see langword="false"/> for blank input, malformed JSON, trailing content, or an invalid node shape.</summary>
    public static bool TryParse(string? value, IFormatProvider? provider, [NotNullWhen(true)] out FilterNode? result)
    {
        result = null;
        if (value is null || string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            // JsonDocument.Parse reads exactly one value; anything after it means the parameter was not one tree.
            using var document = JsonDocument.Parse(value);
            result = FilterNodeJsonConverter.ReadNode(document.RootElement);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
