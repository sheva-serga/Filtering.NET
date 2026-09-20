namespace Filtering.Net.Generator;

// One user-declared profile that needs a runtime FilterProfile<T> instance emitted for it.
// BaseProfileReference is a C# expression yielding the BasedOn profile's runtime instance, or null for standalone profiles.
internal sealed record ProfileBridgeModel(
    string ProfileFullName,
    string ProfileName,
    string BridgeClassName,
    string ColumnTypeFqn,
    string? BaseProfileReference,
    EquatableList<ProfileBridgeOperatorModel> Operators);

// OperatorFactoryCall is the complete FilterOperator.* invocation for the operator.
internal sealed record ProfileBridgeOperatorModel(
    string OperatorName,
    string OperatorFactoryCall);
