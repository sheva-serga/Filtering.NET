namespace Filtering.Net.Generator;

/// <summary>
/// Cacheable model describing one <c>[FilterOperator]</c>-marked member on a custom profile,
/// pre-extracted by <see cref="ProfileResolver"/> so the emitter can inline the lambda body
/// without re-walking syntax. Built-in profile operators (StringFilter etc.) do not produce
/// instances of this model — those go through <see cref="BuiltInProfileCatalog"/> instead.
/// </summary>
/// <param name="OperatorName">The operator key as it appears in filter requests (e.g., <c>"fuzzy"</c>).</param>
/// <param name="DeclaringProfileFullName">The profile that declares this operator (after BasedOn merging,
/// the most-derived declarer wins).</param>
/// <param name="ColumnParameterName">The first lambda parameter name (the column expression).</param>
/// <param name="ValueParameterName">The second lambda parameter name (the user-supplied value), or
/// null when the operator's lambda is unary (column-only, e.g., <c>isNull</c>-shape custom operators).</param>
/// <param name="ValueClrType">The CLR type of the value parameter (or null when unary). Carries
/// the <c>global::</c>-prefixed display string ready for emission.</param>
/// <param name="IsArrayValue">True when <see cref="ValueClrType"/> is an array type — the emitter
/// switches to <c>TryGet…Array</c> extraction in that case (mirrors built-in <c>in</c> shape).</param>
/// <param name="LambdaBodyCSharp">The raw lambda body source (no substitutions). The emitter
/// rewrites the column parameter to the property accessor and the value parameter to the
/// generated value-variable name when emitting the leaf method. May be empty if extraction
/// failed (the emitter then falls back to a throwing stub).</param>
/// <param name="Location">Source location of the <c>[FilterOperator]</c>-marked member
/// declaration, used so FN1008 squiggles appear at the declaration site rather than at
/// <c>Location.None</c>. Null when the declaration site could not be determined.</param>
internal sealed record CustomOperatorModel(
    string OperatorName,
    string DeclaringProfileFullName,
    string ColumnParameterName,
    string? ValueParameterName,
    string? ValueClrType,
    bool IsArrayValue,
    string LambdaBodyCSharp,
    LocationInfo? Location);
