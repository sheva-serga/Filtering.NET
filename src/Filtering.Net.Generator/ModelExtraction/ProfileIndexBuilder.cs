using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

/// <summary>Walks every named type in a <see cref="Compilation"/> looking for
/// <c>[FilterProfile&lt;T&gt;]</c> attributes; returns a <see cref="ProfileIndex"/>
/// keyed by the closed CLR type's display string.</summary>
internal static class ProfileIndexBuilder
{
    private const string FilterProfileAttributeOpenName = "Filtering.Net.FilterProfileAttribute<T>";

    public static ProfileIndex Build(
        Compilation compilation,
        IReadOnlyList<INamedTypeSymbol>? virtualEnumProfiles = null)
    {
        var entries = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var type in EnumerateTypes(compilation.GlobalNamespace))
        {
            foreach (var attribute in type.GetAttributes())
            {
                var attrClass = attribute.AttributeClass;
                if (attrClass is null) continue;
                if (attrClass.OriginalDefinition?.ToDisplayString() != FilterProfileAttributeOpenName) continue;
                if (attrClass.TypeArguments.Length != 1) continue;
                var clrTypeKey = attrClass.TypeArguments[0].ToDisplayString();
                if (!entries.TryGetValue(clrTypeKey, out var bucket))
                {
                    bucket = [];
                    entries[clrTypeKey] = bucket;
                }
                bucket.Add(type.ToDisplayString());
            }
        }

        if (virtualEnumProfiles is not null)
        {
            foreach (var enumSymbol in virtualEnumProfiles)
            {
                var clrTypeKey = enumSymbol.ToDisplayString();
                var profileFullName = $"{EnumProfileEmitter.GeneratedNamespace}.{enumSymbol.Name}Filter";
                if (!entries.TryGetValue(clrTypeKey, out var bucket))
                {
                    bucket = [];
                    entries[clrTypeKey] = bucket;
                }
                bucket.Add(profileFullName);
            }
        }

        return new ProfileIndex(entries);
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol root)
    {
        foreach (var member in root.GetMembers())
        {
            if (member is INamespaceSymbol child)
            {
                foreach (var nested in EnumerateTypes(child))
                {
                    yield return nested;
                }
            }
            else if (member is INamedTypeSymbol type)
            {
                yield return type;
                foreach (var nested in type.GetTypeMembers())
                {
                    yield return nested;
                }
            }
        }
    }
}
