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
                foreach (var typeOrNestedType in EnumerateWithNestedTypes(type))
                {
                    yield return typeOrNestedType;
                }
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateWithNestedTypes(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nestedType in type.GetTypeMembers())
        {
            foreach (var typeOrNestedType in EnumerateWithNestedTypes(nestedType))
            {
                yield return typeOrNestedType;
            }
        }
    }
}
