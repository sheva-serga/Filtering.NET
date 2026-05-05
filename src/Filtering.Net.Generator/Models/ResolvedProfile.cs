namespace Filtering.Net.Generator;

/// <summary>The result of resolving a profile (built-in or explicit) for a property mapping.
/// Carries the profile's full type name, its declared operator list (after BasedOn merging),
/// and pre-extracted lambda metadata for any custom operators (built-in profiles return an
/// empty list because their emission goes through <see cref="BuiltInProfileCatalog"/>).</summary>
internal sealed record ResolvedProfile(
    string ProfileFullName,
    IReadOnlyList<string> Operators,
    IReadOnlyList<CustomOperatorModel> CustomOperators);
