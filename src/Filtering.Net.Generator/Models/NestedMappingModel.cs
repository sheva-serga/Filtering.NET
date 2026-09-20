namespace Filtering.Net.Generator;

// ResolvedTargetClassFqn is filled in by NestedFilterResolver; it stays null when resolution reported a diagnostic.
internal sealed record NestedMappingModel(
    string NavigationPropertyName,
    string Prefix,
    string? ExplicitFilterClassFqn,
    EquatableList<string> Only,
    EquatableList<string> Except,
    bool DisableSorting,
    LocationInfo? AttributeLocation,
    LocationInfo? HostMethodLocation,
    string HostMethodName,
    string? ResolvedTargetClassFqn = null);
