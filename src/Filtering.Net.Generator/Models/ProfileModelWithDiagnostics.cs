namespace Filtering.Net.Generator;

// Model is null when the target symbol wasn't a named class (diagnostics still reported).
internal sealed record ProfileModelWithDiagnostics(
    ProfileModel? Model,
    EquatableList<DiagnosticInfo> Diagnostics);
