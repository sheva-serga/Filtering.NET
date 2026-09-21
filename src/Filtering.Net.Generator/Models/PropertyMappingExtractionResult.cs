namespace Filtering.Net.Generator;

internal sealed record PropertyDeclarationExtractionResult(
    PropertyDeclarationModel? Model,
    IReadOnlyList<DiagnosticInfo> Diagnostics);

internal sealed record PropertyMappingExtractionResult(
    PropertyMappingModel? Model,
    IReadOnlyList<DiagnosticInfo> Diagnostics);
