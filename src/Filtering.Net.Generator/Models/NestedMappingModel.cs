namespace Filtering.Net.Generator;

// MaxDepth zero means unbounded. NestingKey (declaring class + method) and ResolvedTargetClassFqn are filled in by
// NestedFilterResolver; the target stays null when resolution reported a diagnostic.
internal sealed record NestedMappingModel(
    string NavigationPropertyName,
    string Prefix,
    string? ExplicitFilterClassFqn,
    EquatableList<string> Only,
    EquatableList<string> Except,
    bool DisableSorting,
    LocationInfo? AttributeLocation,
    int MaxDepth = 0,
    string? NestingKey = null,
    string? ResolvedTargetClassFqn = null);
