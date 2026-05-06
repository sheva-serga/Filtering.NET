namespace Filtering.Net.Generator;

internal sealed record PropertyMappingExtractionResult(
    PropertyMappingModel? Model,
    IReadOnlyList<DiagnosticInfo> Diagnostics);
