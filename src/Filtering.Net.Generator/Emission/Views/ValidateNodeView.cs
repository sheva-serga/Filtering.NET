namespace Filtering.Net.Generator;

/// <param name="ThreadsSerializerOptions">True when at least one property in this filter class
/// has a typed-value operator — meaning the <c>ValidateLeaf</c> and <c>ValidateNode</c> helpers
/// must accept and thread a <c>JsonSerializerOptions options</c> parameter.</param>
internal sealed record ValidateNodeView(
    bool ThreadsSerializerOptions,
    IReadOnlyList<ValidateLeafArmView> LeafArms);

/// <param name="ArmThreadsOptions">True when this leaf arm's per-property <c>Validate</c>
/// method accepts a <c>JsonSerializerOptions options</c> parameter (i.e., the property has at
/// least one typed-value operator).</param>
internal sealed record ValidateLeafArmView(
    string PropertyIdentifier,
    string PrimaryFieldKey,
    string? AliasFieldKey,
    bool ArmThreadsOptions);
