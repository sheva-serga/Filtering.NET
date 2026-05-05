using System.Text.Json;
using System.Text.Json.Serialization;

namespace Filtering.Net;

internal sealed class FilterNodeJsonConverter : JsonConverter<FilterNode>
{
    public override FilterNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"FilterNode expects an object, got {reader.TokenType}.");

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var hasAnd = root.TryGetProperty("and", out var andElement);
        var hasOr = root.TryGetProperty("or", out var orElement);
        var hasNot = root.TryGetProperty("not", out var notElement);
        var hasField = root.TryGetProperty("field", out _);

        var groupKindCount = (hasAnd ? 1 : 0) + (hasOr ? 1 : 0) + (hasNot ? 1 : 0);

        if (groupKindCount > 1)
            throw new JsonException("FilterGroup must have exactly one of `and`, `or`, or `not`.");
        if (groupKindCount == 1 && hasField)
            throw new JsonException("FilterNode is ambiguous: looks like both group and leaf.");

        if (hasAnd) return ReadGroup(LogicalOp.And, andElement, options);
        if (hasOr) return ReadGroup(LogicalOp.Or, orElement, options);
        if (hasNot) return ReadNotGroup(notElement, options);
        if (hasField) return ReadLeaf(root);

        throw new JsonException("FilterNode requires either `and`/`or`/`not` (group) or `field`/`op`/`value` (leaf).");
    }

    private static FilterGroup ReadGroup(LogicalOp op, JsonElement childrenElement, JsonSerializerOptions options)
    {
        if (childrenElement.ValueKind != JsonValueKind.Array)
            throw new JsonException($"`{op.ToString().ToLowerInvariant()}` must be an array of FilterNodes.");

        var childList = new List<FilterNode>();
        foreach (var childElement in childrenElement.EnumerateArray())
        {
            var child = childElement.Deserialize<FilterNode>(options)
                ?? throw new JsonException("Null child in filter group.");
            childList.Add(child);
        }
        return new FilterGroup(op, childList);
    }

    private static FilterGroup ReadNotGroup(JsonElement notElement, JsonSerializerOptions options)
    {
        FilterGroup readAsArray = ReadGroup(LogicalOp.Not, notElement, options);
        if (readAsArray.Children.Count != 1)
            throw new JsonException("`not` requires exactly one child.");
        return readAsArray;
    }

    private static FilterLeaf ReadLeaf(JsonElement root)
    {
        if (!root.TryGetProperty("field", out var fieldElement) || fieldElement.ValueKind != JsonValueKind.String)
            throw new JsonException("FilterLeaf requires a string `field`.");
        if (!root.TryGetProperty("op", out var opElement) || opElement.ValueKind != JsonValueKind.String)
            throw new JsonException("FilterLeaf requires a string `op`.");
        if (!root.TryGetProperty("value", out var valueElement))
            valueElement = default;
        return new FilterLeaf(fieldElement.GetString()!, opElement.GetString()!, valueElement.Clone());
    }

    public override void Write(Utf8JsonWriter writer, FilterNode value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case FilterLeaf leaf:
                writer.WriteStartObject();
                writer.WriteString("field", leaf.Field);
                writer.WriteString("op", leaf.Operator);
                writer.WritePropertyName("value");
                leaf.Value.WriteTo(writer);
                writer.WriteEndObject();
                break;
            case FilterGroup group:
                writer.WriteStartObject();
                var key = group.Op switch
                {
                    LogicalOp.And => "and",
                    LogicalOp.Or => "or",
                    LogicalOp.Not => "not",
                    _ => throw new JsonException($"Unknown LogicalOp: {group.Op}")
                };
                writer.WritePropertyName(key);
                writer.WriteStartArray();
                foreach (var child in group.Children)
                    JsonSerializer.Serialize(writer, child, options);
                writer.WriteEndArray();
                writer.WriteEndObject();
                break;
            default:
                throw new JsonException($"Unknown FilterNode subtype: {value.GetType().Name}");
        }
    }
}
