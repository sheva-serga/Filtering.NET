namespace Filtering.Net.Generator;

// PropertyAccessorBodyCSharp is empty when the For(...) body could not be parsed; emitter uses a stub.
// HasTypedValueOperator gates JsonSerializerOptions threading; unary operators never set it.
internal sealed record PropertyOverrideModel(
    string PropertyName,
    string MethodName,
    string PropertyAccessorBodyCSharp,
    string EntityParameterName,
    EquatableList<OverrideOperatorModel> Operators,
    bool HasTypedValueOperator);
