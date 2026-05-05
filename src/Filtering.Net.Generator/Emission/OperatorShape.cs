namespace Filtering.Net.Generator;

/// <summary>
/// Categorisation of a built-in profile operator by the shape of its right-hand value: scalar
/// (eq / gt / contains / ...), array (in), or none (isNull). Drives both validation
/// (which TryGet helper to call) and ApplyFilter emission (which leaf method signature to use).
/// </summary>
internal enum OperatorShape
{
    /// <summary>Single scalar value of the property's CLR type (or <c>string</c> for string ops).</summary>
    Scalar,
    /// <summary>JSON array of values (the <c>in</c> operator).</summary>
    Array,
    /// <summary>No value at all (the <c>isNull</c> operator).</summary>
    None,
}
