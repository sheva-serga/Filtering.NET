using System.Text.Json;

namespace Filtering.Net;

internal readonly struct FilterValueContext(JsonSerializerOptions? serializerOptions)
{
    public JsonSerializerOptions? SerializerOptions { get; } = serializerOptions;
}
