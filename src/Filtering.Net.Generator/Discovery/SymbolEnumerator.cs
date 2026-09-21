using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

internal static class SymbolEnumerator
{
    public static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol root)
    {
        foreach (var member in root.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                foreach (var nestedType in EnumerateTypes(childNamespace))
                {
                    yield return nestedType;
                }
            }
            else if (member is INamedTypeSymbol type)
            {
                yield return type;
                foreach (var nestedType in type.GetTypeMembers())
                {
                    yield return nestedType;
                }
            }
        }
    }
}
