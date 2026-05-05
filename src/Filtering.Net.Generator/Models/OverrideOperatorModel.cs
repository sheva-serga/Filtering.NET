namespace Filtering.Net.Generator;

/// <summary>Model describing a single operator declared inside a <c>[PropertyMap]</c> method body
/// via <c>builder.Operator("name", (column, value) =&gt; …)</c>.</summary>
/// <param name="Name">The operator key as it appears in filter requests.</param>
/// <param name="ColumnParameterName">First lambda parameter name (the property's value).</param>
/// <param name="ValueParameterName">Second lambda parameter name (the user-supplied value), or
/// null when the operator's lambda is unary.</param>
/// <param name="ValueClrType">CLR display string of <c>TArgument</c> in the predicate
/// <c>Expression&lt;Func&lt;TValue, TArgument, bool&gt;&gt;</c>, or null when unary. Already
/// formatted with <c>global::</c> where needed for emission.</param>
/// <param name="PredicateBodyCSharp">The lambda body source. The emitter substitutes the
/// column parameter with the property accessor and the value parameter with the leaf method's
/// value variable when emitting the typed leaf.</param>
/// <param name="Location">Source location of the <c>.Operator(...)</c> call within the
/// <c>[PropertyMap]</c> method body, used so FN1008 squiggles appear at the declaration
/// site rather than at <c>Location.None</c>. Null when the site could not be determined.</param>
internal sealed record OverrideOperatorModel(
    string Name,
    string ColumnParameterName,
    string? ValueParameterName,
    string? ValueClrType,
    string PredicateBodyCSharp,
    LocationInfo? Location);
