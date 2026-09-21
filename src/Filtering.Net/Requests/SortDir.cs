using System.Text.Json.Serialization;

namespace Filtering.Net;

/// <summary>Direction for a <see cref="SortItem"/>. Read from JSON as the member name (case-insensitive) or its number; written as the name.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SortDir>))]
public enum SortDir
{
    /// <summary>Ascending order.</summary>
    Asc,
    /// <summary>Descending order.</summary>
    Desc
}
