using System.Text.Json.Serialization;

namespace Filtering.Net;

/// <summary>Logical combinator for a <see cref="FilterGroup"/>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<LogicalOp>))]
public enum LogicalOp
{
    /// <summary>All children must match.</summary>
    And,
    /// <summary>At least one child must match.</summary>
    Or,
    /// <summary>The single child must not match.</summary>
    Not
}
