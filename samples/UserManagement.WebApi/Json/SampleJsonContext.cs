using System.Text.Json.Serialization;

namespace UserManagement.WebApi.Json;

// Trim/AOT-safe resolver passed to AddFiltering. Add a [JsonSerializable] entry per typed-value type used by custom operators.
[JsonSerializable(typeof(string))]
public partial class SampleJsonContext : JsonSerializerContext;
