namespace Filtering.Net.Generator;

/// <summary>
/// Tuple of (model, diagnostics) returned from extraction. Both flow through the incremental
/// pipeline and are unpacked by <c>RegisterSourceOutput</c>: diagnostics are always reported,
/// while the model is only used for emission when non-null.
/// </summary>
internal sealed record FilterClassModelWithDiagnostics(
    FilterClassModel? Model,
    EquatableList<DiagnosticInfo> Diagnostics);
