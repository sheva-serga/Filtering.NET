namespace Filtering.Net.Generator;

/// <summary>
/// Cacheable model describing a discovered <c>[FilterProfile]</c>-marked class. Carries enough
/// information for cross-class checks (FN1003 ProfileUnused, FN1004 OperatorUnused) without
/// holding any non-equatable Roslyn types.
/// </summary>
internal sealed record ProfileModel(
    string ProfileFullName,
    EquatableList<string> OperatorNames,
    LocationInfo? Location);
