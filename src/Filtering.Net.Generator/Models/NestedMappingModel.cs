namespace Filtering.Net.Generator;

// How the [MapNested] navigation resolved on the declaring entity. Classified while the entity
// symbol is in hand so NestedFilterResolver works on strings only.
internal enum NestedNavigationKind
{
    Missing,
    PrimitiveOrValue,
    Collection,
    Reference,
}

// MaxDepth zero means unbounded. HasOnly separates an explicitly empty Only (allow nothing) from an absent one (no whitelist).
// NestingKey (declaring class + navigation) and ResolvedTargetClassFqn are filled in by
// NestedFilterResolver; the target stays null when resolution reported a diagnostic.
internal sealed record NestedMappingModel(
    string NavigationPropertyName,
    string Prefix,
    string? ExplicitFilterClassFqn,
    EquatableList<string> Only,
    EquatableList<string> Except,
    bool DisableSorting,
    LocationInfo? AttributeLocation,
    NestedNavigationKind NavigationKind = NestedNavigationKind.Missing,
    string? NavigationTypeFullName = null,
    LocationInfo? NavigationLocation = null,
    LocationInfo? ExplicitFilterClassLocation = null,
    int MaxDepth = 0,
    string? NestingKey = null,
    string? ResolvedTargetClassFqn = null,
    bool HasOnly = false);
