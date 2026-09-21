namespace Filtering.Net.Generator;

// What one [Map] says, before any profile is resolved. PropertyClrTypeKey is the leaf type with
// Nullable<T> unwrapped — the key both the profile index and the compatibility check are stated in.
// OnlyOperators/ExceptOperators are empty when the named argument was absent (HasOnly/HasExcept
// false); an explicitly empty array is a restriction that allows nothing, not an absent one.
internal sealed record PropertyDeclarationModel(
    string PropertyName,
    string PropertyClrType,
    string PropertyClrTypeKey,
    bool IsNullableValueType,
    string? ExplicitProfileFullName,
    LocationInfo? ExplicitProfileLocation,
    EquatableList<string> OnlyOperators,
    bool HasOnly,
    EquatableList<string> ExceptOperators,
    bool HasExcept,
    string? Alias,
    bool Sortable,
    string DefaultSortDirection,
    LocationInfo? DeclarationLocation,
    LocationInfo? LeafPropertyLocation);

// The symbol-derived half of a filter class: everything readable from the class's own syntax tree.
// FilterClassResolver turns it into a FilterClassModel once the compilation-wide GeneratorIndex is
// combined in, so nothing here depends on state Roslyn does not track for this tree.
internal sealed record FilterClassDeclaration(
    string Namespace,
    string ClassName,
    string FullEntityTypeName,
    int? ClassDefaultPageSize,
    int? ClassMaxPageSize,
    EquatableList<PropertyDeclarationModel> Properties,
    EquatableList<InterceptorModel> Interceptors,
    EquatableList<PropertyOverrideModel> Overrides,
    LocationInfo? Location,
    EquatableList<NestedMappingModel> NestedMappings);

internal sealed record FilterClassDeclarationWithDiagnostics(
    FilterClassDeclaration? Declaration,
    EquatableList<DiagnosticInfo> Diagnostics);
