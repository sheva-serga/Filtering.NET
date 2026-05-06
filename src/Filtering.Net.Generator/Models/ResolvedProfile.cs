namespace Filtering.Net.Generator;

// Built-in profiles return an empty CustomOperators list; emission goes through BuiltInProfileCatalog.
internal sealed record ResolvedProfile(
    string ProfileFullName,
    IReadOnlyList<string> Operators,
    IReadOnlyList<CustomOperatorModel> CustomOperators);
