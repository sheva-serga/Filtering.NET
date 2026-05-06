namespace Filtering.Net.Generator;

// Carries enough for cross-pipeline checks (FN1003, FN1004) with no non-equatable Roslyn types.
internal sealed record ProfileModel(
    string ProfileFullName,
    EquatableList<string> OperatorNames,
    LocationInfo? Location);
