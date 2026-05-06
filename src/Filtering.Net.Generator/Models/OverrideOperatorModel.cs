namespace Filtering.Net.Generator;

internal sealed record OverrideOperatorModel(
    string Name,
    string ColumnParameterName,
    string? ValueParameterName,
    string? ValueClrType,
    string PredicateBodyCSharp,
    LocationInfo? Location);
