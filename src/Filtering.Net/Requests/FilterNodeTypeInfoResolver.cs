using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Filtering.Net;

internal sealed class FilterNodeTypeInfoResolver : IJsonTypeInfoResolver
{
    // Nested groups deserialize their children through the options, so the resolver must know FilterNode without reflection for Native AOT.
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) =>
        type == typeof(FilterNode)
            ? JsonMetadataServices.CreateValueInfo<FilterNode>(options, new FilterNodeJsonConverter())
            : null;
}
