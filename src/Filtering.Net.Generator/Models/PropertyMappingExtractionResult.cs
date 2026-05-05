namespace Filtering.Net.Generator;

/// <summary>The result of extracting a single <c>[Map]</c>-decorated method into a <see cref="PropertyMappingModel"/>. Returns the model (when extraction succeeded) plus any diagnostics raised along the way.</summary>
internal sealed record PropertyMappingExtractionResult(
    PropertyMappingModel? Model,
    IReadOnlyList<DiagnosticInfo> Diagnostics);
