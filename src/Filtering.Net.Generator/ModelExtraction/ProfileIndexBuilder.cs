using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// One [FilterProfile<TColumn>] declaration found in the compilation. Symbols never leave the
// index build, so this shape stays out of the incremental pipeline.
internal readonly struct ProfileTypeBinding(string clrTypeKey, INamedTypeSymbol profileType)
{
    public string ClrTypeKey { get; } = clrTypeKey;

    public INamedTypeSymbol ProfileType { get; } = profileType;
}

internal static class ProfileIndexBuilder
{
    private const string FilterProfileAttributeOpenName = "Filtering.Net.FilterProfileAttribute<T>";

    public static ProfileIndex Build(Compilation compilation) =>
        BuildFrom(CollectProfileTypes(compilation), enumProfiles: null);

    // Referenced assemblies may declare shared profiles, so this deliberately walks the merged
    // global namespace rather than just the source assembly.
    public static List<ProfileTypeBinding> CollectProfileTypes(Compilation compilation)
    {
        var bindings = new List<ProfileTypeBinding>();
        foreach (var type in SymbolEnumerator.EnumerateTypes(compilation.GlobalNamespace))
        {
            foreach (var attribute in type.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass is null) continue;
                if (attributeClass.OriginalDefinition?.ToDisplayString() != FilterProfileAttributeOpenName) continue;
                if (attributeClass.TypeArguments.Length != 1) continue;
                bindings.Add(new ProfileTypeBinding(attributeClass.TypeArguments[0].ToDisplayString(), type));
            }
        }
        return bindings;
    }

    // The auto-emitted enum profiles arrive as descriptors rather than symbols so their generated
    // names are decided in exactly one place (GeneratorIndexBuilder) and cannot drift from the
    // names the emitter and the AddSource hint use.
    public static ProfileIndex BuildFrom(
        IReadOnlyList<ProfileTypeBinding> bindings,
        IReadOnlyList<EnumProfileDescriptor>? enumProfiles)
    {
        var entries = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var binding in bindings)
        {
            AddEntry(entries, binding.ClrTypeKey, binding.ProfileType.ToDisplayString());
        }

        if (enumProfiles is not null)
        {
            foreach (var enumProfile in enumProfiles)
            {
                AddEntry(entries, enumProfile.EnumFullName, enumProfile.ProfileFullName);
            }
        }

        return new ProfileIndex(entries);
    }

    private static void AddEntry(Dictionary<string, List<string>> entries, string clrTypeKey, string profileFullName)
    {
        if (!entries.TryGetValue(clrTypeKey, out var bucket))
        {
            bucket = [];
            entries[clrTypeKey] = bucket;
        }
        bucket.Add(profileFullName);
    }
}
