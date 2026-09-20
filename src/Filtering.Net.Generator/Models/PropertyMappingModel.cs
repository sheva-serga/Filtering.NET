namespace Filtering.Net.Generator;

// PropertyName is a verbatim CLR accessor path ("Department.Name"); the emitter splices it after "entity.".
// DeclarationName names the declaring attribute in diagnostics: the property for [Map], the navigation for a spliced [MapNested].
internal sealed record PropertyMappingModel(
    string PropertyName,
    string PropertyClrType,
    bool IsNullableValueType,
    string ProfileFullName,
    EquatableList<string> AllowedOperators,
    bool HasOperatorRestriction,
    string? Alias,
    bool Sortable,
    string DefaultSortDirection,
    string DeclarationName,
    EquatableList<CustomOperatorModel> CustomOperators,
    bool HasTypedValueOperator,
    EquatableList<ProfileBridgeModel> ProfileBridges,
    LocationInfo? DeclarationLocation = null,
    LocationInfo? InliningSiteLocation = null,
    string? SourceFilterClassFqn = null);
