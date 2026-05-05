namespace Filtering.Net.Generator;

/// <param name="ThreadsSerializerOptions">True when the filter class has at least one
/// typed-value property; threads <c>JsonSerializerOptions options</c> through
/// <c>BuildPredicate</c>, <c>CombineGroup</c>, and <c>BuildLeaf</c>.</param>
internal sealed record ApplyFilterView(
    string EntityFullName,
    bool ThreadsSerializerOptions,
    IReadOnlyList<DispatchArmView> DispatchArms);

/// <param name="ArmThreadsOptions">True when this per-property nested class's <c>Build</c>
/// method accepts <c>JsonSerializerOptions options</c> — i.e., the property has at least one
/// typed-value operator.</param>
internal sealed record DispatchArmView(
    string PropertyIdentifier,
    string PrimaryFieldKey,
    string? AliasFieldKey,
    bool ArmThreadsOptions);
