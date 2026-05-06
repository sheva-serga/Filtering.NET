namespace Filtering.Net.Generator;

// Diagnostics are always reported; model is only used for emission when non-null.
internal sealed record FilterClassModelWithDiagnostics(
    FilterClassModel? Model,
    EquatableList<DiagnosticInfo> Diagnostics);
