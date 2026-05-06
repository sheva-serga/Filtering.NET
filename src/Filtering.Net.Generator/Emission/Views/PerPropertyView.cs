namespace Filtering.Net.Generator;

internal sealed record PerPropertyView(
    string PropertyIdentifier,
    string PropertyName,
    string PropertyKindLabel,
    bool IsOverrideKind,
    bool OverrideHasUsableBody,
    string EntityFullName,
    bool BuildThreadsOptions,
    bool ValidateThreadsOptions,
    PerPropertyBuildView Build,
    PerPropertyValidateView Validate,
    IReadOnlyList<string> TypedLeafMethods,
    PerPropertySortView? Sort);

// Accessor is the entity-rooted column expression (e.g. entity.Name) used in OrderBy / ThenBy arms.
internal sealed record PerPropertySortView(string Accessor);

internal sealed record PerPropertyBuildView(
    bool IsOverride,
    PerPropertyScalarArmView? ScalarArm,
    PerPropertyArrayArmView? ArrayArm,
    IReadOnlyList<string> NoneOperators,
    IReadOnlyList<PerPropertyCustomBuildArmView> CustomBuildArms,
    IReadOnlyList<PerPropertyOverrideBuildArmView> OverrideBuildArms);

internal sealed record PerPropertyScalarArmView(
    IReadOnlyList<string> OperatorKeys,
    string ExtractCall,
    bool HasInterceptor,
    string? InterceptorMethodQualified,
    IReadOnlyList<PerPropertyOperatorDispatchView> Dispatch);

internal sealed record PerPropertyArrayArmView(
    IReadOnlyList<string> OperatorKeys,
    string ExtractCall,
    string ArgExpression,
    IReadOnlyList<PerPropertyOperatorDispatchView> Dispatch);

internal sealed record PerPropertyOperatorDispatchView(
    string OperatorKeyUpper,
    string OperatorIdentifier);

internal sealed record PerPropertyCustomBuildArmView(
    string OperatorKeyUpper,
    string OperatorIdentifier,
    bool IsUnary,
    string? ValueClrType,
    string OperatorName);

internal sealed record PerPropertyOverrideBuildArmView(
    string OperatorKeyUpper,
    string OperatorIdentifier,
    bool IsUnary,
    string? ValueClrType,
    string OperatorName);

internal sealed record PerPropertyValidateView(
    bool IsOverride,
    PerPropertyScalarValidateArmView? ScalarArm,
    PerPropertyArrayValidateArmView? ArrayArm,
    IReadOnlyList<string> NoneOperators,
    IReadOnlyList<PerPropertyCustomValidateArmView> CustomValidateArms,
    IReadOnlyList<PerPropertyOverrideValidateArmView> OverrideValidateArms,
    SimpleValidateForwarderView? SimpleForwarder);

internal sealed record SimpleValidateForwarderView(
    string ScalarValueClrType,
    string ScalarOpsArrayLiteral,
    string ArrayOpsArrayLiteral,
    string NoneOpsArrayLiteral,
    string? ScalarExtractorMethodGroup,
    string? ArrayExtractorMethodGroup);

internal sealed record PerPropertyScalarValidateArmView(
    IReadOnlyList<string> OperatorKeys,
    string ExtractCall,
    bool HasInterceptor,
    string? InterceptorMethodQualified,
    string PropertyName);

internal sealed record PerPropertyArrayValidateArmView(
    IReadOnlyList<string> OperatorKeys,
    string ExtractCall);

internal sealed record PerPropertyCustomValidateArmView(
    string OperatorKeyUpper,
    bool IsUnary,
    string? ValueClrType);

internal sealed record PerPropertyOverrideValidateArmView(
    string OperatorKeyUpper,
    bool IsUnary,
    string? ValueClrType);
