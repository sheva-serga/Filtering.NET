using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Filtering.Net;

/// <summary>Base type for filter expressions. Concrete types: <see cref="FilterGroup"/>, <see cref="FilterLeaf"/>.</summary>
[JsonConverter(typeof(FilterNodeJsonConverter))]
public abstract record FilterNode
{
    // The same converter the body path uses, called directly so query-string parsing adds no reflection-based serializer call.
    private static readonly FilterNodeJsonConverter QueryStringConverter = new();
    private static readonly JsonSerializerOptions QueryStringSerializerOptions = new() { TypeInfoResolver = new FilterNodeTypeInfoResolver() };

    /// <summary>Parses a filter tree from its JSON text, as sent in a query-string <c>where</c> parameter; returns <see langword="false"/> for blank input, malformed JSON, trailing content, or an invalid node shape.</summary>
    public static bool TryParse(string? value, IFormatProvider? provider, [NotNullWhen(true)] out FilterNode? result)
    {
        result = null;
        if (value is null || string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(value));
            if (!reader.Read()) return false;
            var parsedNode = QueryStringConverter.Read(ref reader, typeof(FilterNode), QueryStringSerializerOptions);
            // The converter consumes exactly one value; anything after it means the parameter was not one tree.
            if (reader.Read()) return false;
            result = parsedNode;
            return result is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
