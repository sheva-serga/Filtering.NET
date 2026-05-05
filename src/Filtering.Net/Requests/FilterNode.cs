using System.Text.Json.Serialization;

namespace Filtering.Net;

/// <summary>Base type for filter expressions. Concrete types: <see cref="FilterGroup"/>, <see cref="FilterLeaf"/>.</summary>
[JsonConverter(typeof(FilterNodeJsonConverter))]
public abstract record FilterNode;
