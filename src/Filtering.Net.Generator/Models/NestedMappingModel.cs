namespace Filtering.Net.Generator;

internal sealed record NestedMappingModel(
    string NavigationPropertyName,
    string Prefix,
    string? ExplicitFilterClassFqn,
    EquatableList<string> Only,
    EquatableList<string> Except,
    bool DisableSorting,
    LocationInfo? AttributeLocation,
    LocationInfo? HostMethodLocation,
    string HostMethodName);
