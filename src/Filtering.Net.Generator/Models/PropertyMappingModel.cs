namespace Filtering.Net.Generator;

internal sealed record PropertyMappingModel(
    string PropertyName,
    string PropertyClrType,
    string ProfileFullName,
    string ExtractorProfileFullName,
    EquatableList<string> AllowedOperators,
    string? Alias,
    bool Sortable,
    string DefaultSortDirection,
    string ConfigurationMethodName,
    EquatableList<CustomOperatorModel> CustomOperators,
    bool HasTypedValueOperator);
