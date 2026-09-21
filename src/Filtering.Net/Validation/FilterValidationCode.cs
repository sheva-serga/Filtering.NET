using System.Text.Json.Serialization;

namespace Filtering.Net;

/// <summary>Categorizes a <see cref="FilterValidationError"/>. Written to JSON as the member name; the numeric values are pinned for callers that persisted them.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterValidationCode>))]
public enum FilterValidationCode
{
    /// <summary>Field name not configured for filtering.</summary>
    UnknownField = 0,
    /// <summary>Operator not in the property's profile / excluded by Only/Except.</summary>
    OperatorNotAllowed = 1,
    /// <summary>Value the operator cannot read: wrong JsonValueKind, or the right kind in a format the column type rejects.</summary>
    InvalidValueType = 2,
    // 3 and 4 were InvalidValueFormat and EmptyInArray, which nothing produced; the gap keeps the other values stable.
    /// <summary>Interceptor threw FilterValidationException.</summary>
    InterceptorRejected = 5,
    /// <summary>Sort item names no field, or names one that is not configured as sortable.</summary>
    NotSortable = 6,
    /// <summary>Sort direction value not Asc/Desc.</summary>
    InvalidSortDirection = 7,
    /// <summary>page &lt; 1, or a page so large that the rows to skip overflow an <see cref="int"/>.</summary>
    PageInvalid = 8,
    /// <summary>pageSize &gt; MaxPageSize.</summary>
    PageSizeTooLarge = 9,
    /// <summary>pageSize &lt; 1.</summary>
    PageSizeInvalid = 10,
    /// <summary>Filter nesting depth exceeds MaxNestingDepth.</summary>
    NestingTooDeep = 11,
    /// <summary>Total leaf count exceeds MaxLeafConditions.</summary>
    TooManyConditions = 12,
    /// <summary>A group (and, or, not) with zero children.</summary>
    GroupEmpty = 13,
    /// <summary>Node the engine cannot interpret: a not group with more than one child, a combinator outside <see cref="LogicalOp"/>, or an unrecognized <see cref="FilterNode"/> subtype.</summary>
    InvalidNodeShape = 14
}
