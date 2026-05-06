namespace Filtering.Net.Generator;

internal sealed record ValidateNodeView(
    bool ThreadsSerializerOptions,
    IReadOnlyList<ValidateLeafArmView> LeafArms);

internal sealed record ValidateLeafArmView(
    string PropertyIdentifier,
    string PrimaryFieldKey,
    string? AliasFieldKey,
    bool ArmThreadsOptions);
