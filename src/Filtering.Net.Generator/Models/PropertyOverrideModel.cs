namespace Filtering.Net.Generator;

/// <summary>Model describing a per-property override method (a method marked with <c>[PropertyMap]</c>).</summary>
/// <param name="PropertyName">The property name from <c>[PropertyMap("Name")]</c>; used as the
/// dispatcher field key (and as the suffix on emitted <c>Build…</c> methods).</param>
/// <param name="MethodName">The user-declared method's name; carried for diagnostic messages.</param>
/// <param name="PropertyAccessorBodyCSharp">The body of the <c>For(entity =&gt; entity.X)</c> lambda
/// — i.e., the right-hand side after the arrow, ready for emission. Empty when the body could not
/// be parsed or the entity parameter name was not extracted; the emitter falls back to a
/// throwing stub in that case.</param>
/// <param name="EntityParameterName">The first lambda parameter name from the <c>For</c> call;
/// used by the emitter to substitute with the generated entity-variable name.</param>
/// <param name="Operators">Per-operator metadata extracted from the <c>.Operator(…)</c> chain.</param>
/// <param name="HasTypedValueOperator">True when at least one operator in <see cref="Operators"/>
/// has a non-null <see cref="OverrideOperatorModel.ValueClrType"/> — i.e., at least one
/// <c>.Operator("name", (column, value) =&gt; …)</c> call in the <c>[PropertyMap]</c> method
/// body used a typed value parameter. Such operators require the AOT-safe typed-value
/// deserialisation path (<c>JsonSerializer.Deserialize&lt;T&gt;</c>). Unary operators
/// (column-only lambdas) always use the JsonElement-based extractors and leave this flag
/// false.</param>
internal sealed record PropertyOverrideModel(
    string PropertyName,
    string MethodName,
    string PropertyAccessorBodyCSharp,
    string EntityParameterName,
    EquatableList<OverrideOperatorModel> Operators,
    bool HasTypedValueOperator);
