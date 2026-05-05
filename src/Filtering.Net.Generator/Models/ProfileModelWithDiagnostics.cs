namespace Filtering.Net.Generator;

/// <summary>
/// Tuple of (profile model, diagnostics) returned from <see cref="ProfileExtractor.Extract"/>.
/// Diagnostics are always reported; the model is null when extraction couldn't even produce a
/// meaningful name (e.g., the symbol wasn't a class).
/// </summary>
internal sealed record ProfileModelWithDiagnostics(
    ProfileModel? Model,
    EquatableList<DiagnosticInfo> Diagnostics);
