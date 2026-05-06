namespace Filtering.Net.Generator;

internal sealed record ApplyFilterView(
    string EntityFullName,
    bool ThreadsSerializerOptions,
    IReadOnlyList<DispatchArmView> DispatchArms);

internal sealed record DispatchArmView(
    string PropertyIdentifier,
    string PrimaryFieldKey,
    string? AliasFieldKey,
    bool ArmThreadsOptions);
