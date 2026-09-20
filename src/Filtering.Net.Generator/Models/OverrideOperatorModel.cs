namespace Filtering.Net.Generator;

internal sealed record OverrideOperatorModel(
    string Name,
    string? ValueClrType,
    LocationInfo? Location);
