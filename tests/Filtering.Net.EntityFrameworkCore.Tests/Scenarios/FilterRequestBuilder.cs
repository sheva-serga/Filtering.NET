using System.Text.Json;

namespace Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

/// <summary>Lightweight helpers for assembling FilterRequest values inline in tests without
/// repeating the JSON-element ceremony. Each method returns a leaf or group node which the
/// caller wraps in a FilterRequest.</summary>
internal static class FilterRequestBuilder
{
    public static FilterLeaf Leaf(string field, string @operator, object? value)
    {
        var json = value switch
        {
            null => "null",
            string asString => JsonSerializer.Serialize(asString),
            bool asBool => asBool ? "true" : "false",
            _ => JsonSerializer.Serialize(value),
        };
        return new FilterLeaf(field, @operator, JsonDocument.Parse(json).RootElement);
    }

    public static FilterLeaf InLeaf(string field, params object[] values)
    {
        var json = JsonSerializer.Serialize(values);
        return new FilterLeaf(field, "in", JsonDocument.Parse(json).RootElement);
    }

    public static FilterGroup And(params FilterNode[] children) =>
        new(LogicalOp.And, [.. children]);

    public static FilterGroup Or(params FilterNode[] children) =>
        new(LogicalOp.Or, [.. children]);

    public static FilterGroup Not(FilterNode child) =>
        new(LogicalOp.Not, [child]);
}
