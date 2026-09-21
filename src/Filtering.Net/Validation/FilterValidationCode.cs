namespace Filtering.Net;

/// <summary>Categorizes a <see cref="FilterValidationError"/>.</summary>
public enum FilterValidationCode
{
    /// <summary>Field name not configured for filtering.</summary>
    UnknownField,
    /// <summary>Operator not in the property's profile / excluded by Only/Except.</summary>
    OperatorNotAllowed,
    /// <summary>Wrong JsonValueKind (e.g., bool where number expected).</summary>
    InvalidValueType,
    /// <summary>Right kind, wrong format (e.g., "abc" for invariant decimal).</summary>
    InvalidValueFormat,
    /// <summary>"in" operator with empty array.</summary>
    EmptyInArray,
    /// <summary>Interceptor threw FilterValidationException.</summary>
    InterceptorRejected,
    /// <summary>Sort item names no field, or names one that is not configured as sortable.</summary>
    NotSortable,
    /// <summary>Sort direction value not Asc/Desc.</summary>
    InvalidSortDirection,
    /// <summary>page &lt; 1, or a page so large that the rows to skip overflow an <see cref="int"/>.</summary>
    PageInvalid,
    /// <summary>pageSize &gt; MaxPageSize.</summary>
    PageSizeTooLarge,
    /// <summary>pageSize &lt; 1.</summary>
    PageSizeInvalid,
    /// <summary>Filter nesting depth exceeds MaxNestingDepth.</summary>
    NestingTooDeep,
    /// <summary>Total leaf count exceeds MaxLeafConditions.</summary>
    TooManyConditions,
    /// <summary>A group (and, or, not) with zero children.</summary>
    GroupEmpty,
    /// <summary>Node the engine cannot interpret: a not group with more than one child, a combinator outside <see cref="LogicalOp"/>, or an unrecognized <see cref="FilterNode"/> subtype.</summary>
    InvalidNodeShape
}
