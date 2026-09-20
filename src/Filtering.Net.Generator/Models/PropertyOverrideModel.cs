namespace Filtering.Net.Generator;

// BuilderTypeFqn is null when the method cannot be called from generated code (not static, or not
// returning FilterRule<,> from a single FilterRuleBuilder<,> parameter); the emitter then skips it.
// HasTypedValueOperator gates the JSON-resolver constructors; unary operators never set it.
internal sealed record PropertyOverrideModel(
    string PropertyName,
    string MethodName,
    string? BuilderTypeFqn,
    EquatableList<OverrideOperatorModel> Operators,
    bool HasTypedValueOperator);
