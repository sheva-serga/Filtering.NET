namespace Filtering.Net.Generator;

// BuilderTypeFqn is null when the method cannot be called from generated code; SignatureProblem then
// says why, and FilterClassExtractor turns it into FN0023 instead of silently dropping the property.
// HasTypedValueOperator gates the JSON-resolver constructors; unary operators never set it.
internal sealed record PropertyOverrideModel(
    string PropertyName,
    string MethodName,
    string? BuilderTypeFqn,
    EquatableList<OverrideOperatorModel> Operators,
    bool HasTypedValueOperator,
    string? SignatureProblem = null,
    LocationInfo? DeclarationLocation = null);
