namespace Filtering.Net.Generator;

// Assembly-level [FilterDefaults] values. Null means the named argument was absent, so the
// per-class fallback applies; zero and negative values are the consumer's and pass through.
internal sealed record AssemblyFilterDefaults(
    int? DefaultPageSize,
    int? MaxPageSize,
    int? MaxNestingDepth,
    int? MaxLeafConditions)
{
    public static readonly AssemblyFilterDefaults None = new(null, null, null, null);
}

// One [FilterProfile<TColumn>] type, resolved once per compilation: its operator names (including
// the ones inherited through BasedOn), the metadata of the operators that need a runtime bridge,
// and the bridge chain the emitter needs. Auto-emitted enum profiles get an entry with no bridges.
internal sealed record ProfileDefinition(
    string ProfileFullName,
    string ColumnTypeKey,
    EquatableList<string> Operators,
    EquatableList<CustomOperatorModel> CustomOperators,
    EquatableList<ProfileBridgeModel> Bridges,
    LocationInfo? DeclarationLocation);

// An enum reached from a [GenerateFilter] entity; one profile class is emitted per entry.
internal sealed record EnumProfileDescriptor(string EnumFullName, string ProfileFullName, string ClassName);

// Everything a filter class needs to know about the rest of the compilation. Computed once in its
// own CompilationProvider node and combined into the per-class pipeline, so a change that lands in
// another file (an assembly attribute, a new profile) re-runs the classes that depend on it instead
// of serving the model cached from the last time that class's own file was touched.
internal sealed class GeneratorIndex : IEquatable<GeneratorIndex>
{
    private readonly Dictionary<string, ProfileDefinition> _profilesByFullName;

    public GeneratorIndex(
        AssemblyFilterDefaults assemblyDefaults,
        ProfileIndex profilesByClrType,
        EquatableList<ProfileDefinition> profiles,
        EquatableList<EnumProfileDescriptor> enumProfiles,
        bool isDependencyInjectionReferenced,
        bool isFilterValueDiagnosticsOptIn,
        EquatableList<string> registeredJsonTypeNames)
    {
        AssemblyDefaults = assemblyDefaults;
        ProfilesByClrType = profilesByClrType;
        Profiles = profiles;
        EnumProfiles = enumProfiles;
        IsDependencyInjectionReferenced = isDependencyInjectionReferenced;
        IsFilterValueDiagnosticsOptIn = isFilterValueDiagnosticsOptIn;
        RegisteredJsonTypeNames = registeredJsonTypeNames;

        _profilesByFullName = new Dictionary<string, ProfileDefinition>(profiles.Count, StringComparer.Ordinal);
        foreach (var profile in profiles)
        {
            _profilesByFullName[profile.ProfileFullName] = profile;
        }
    }

    public AssemblyFilterDefaults AssemblyDefaults { get; }

    public ProfileIndex ProfilesByClrType { get; }

    public EquatableList<ProfileDefinition> Profiles { get; }

    public EquatableList<EnumProfileDescriptor> EnumProfiles { get; }

    public bool IsDependencyInjectionReferenced { get; }

    // [assembly: FilterValueDiagnostics(WarnUnregistered = true)] — the FN1008 opt-in.
    public bool IsFilterValueDiagnosticsOptIn { get; }

    // Fully-qualified names registered through [JsonSerializable(typeof(X))] on a visible
    // JsonSerializerContext, with and without the global:: prefix. Empty unless opted in.
    public EquatableList<string> RegisteredJsonTypeNames { get; }

    public ProfileDefinition? FindProfile(string profileFullName) =>
        _profilesByFullName.TryGetValue(profileFullName, out var profile) ? profile : null;

    public bool Equals(GeneratorIndex? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return AssemblyDefaults == other.AssemblyDefaults
            && ProfilesByClrType.Equals(other.ProfilesByClrType)
            && Profiles.Equals(other.Profiles)
            && EnumProfiles.Equals(other.EnumProfiles)
            && IsDependencyInjectionReferenced == other.IsDependencyInjectionReferenced
            && IsFilterValueDiagnosticsOptIn == other.IsFilterValueDiagnosticsOptIn
            && RegisteredJsonTypeNames.Equals(other.RegisteredJsonTypeNames);
    }

    public override bool Equals(object? obj) => obj is GeneratorIndex other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + AssemblyDefaults.GetHashCode();
            hash = (hash * 31) + ProfilesByClrType.GetHashCode();
            hash = (hash * 31) + Profiles.GetHashCode();
            hash = (hash * 31) + EnumProfiles.GetHashCode();
            hash = (hash * 31) + IsDependencyInjectionReferenced.GetHashCode();
            hash = (hash * 31) + IsFilterValueDiagnosticsOptIn.GetHashCode();
            hash = (hash * 31) + RegisteredJsonTypeNames.GetHashCode();
            return hash;
        }
    }
}
