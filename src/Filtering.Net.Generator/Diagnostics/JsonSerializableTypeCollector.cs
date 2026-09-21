using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

internal static class JsonSerializableTypeCollector
{
    private const string JsonSerializableAttributeFullName = "System.Text.Json.Serialization.JsonSerializableAttribute";
    private const string JsonSerializerContextFullName = "System.Text.Json.Serialization.JsonSerializerContext";
    private const string SystemTextJsonAssemblyName = "System.Text.Json";

    // Each registration is recorded both with and without the global:: prefix so the comparison
    // against a stored ValueClrType needs no string surgery at the call site.
    public static List<string> CollectRegisteredTypeNames(Compilation compilation)
    {
        var registeredTypeNames = new List<string>();

        var jsonSerializerContextSymbol = compilation.GetTypeByMetadataName(JsonSerializerContextFullName);
        var jsonSerializableAttributeSymbol = compilation.GetTypeByMetadataName(JsonSerializableAttributeFullName);
        if (jsonSerializerContextSymbol is null || jsonSerializableAttributeSymbol is null)
        {
            return registeredTypeNames; // System.Text.Json not referenced — nothing to register.
        }

        foreach (var namedType in EnumerateCandidateTypes(compilation))
        {
            if (!IsSubclassOf(namedType, jsonSerializerContextSymbol)) continue;

            foreach (var attribute in namedType.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, jsonSerializableAttributeSymbol))
                    continue;
                if (attribute.ConstructorArguments.Length == 0)
                    continue;
                // typeof(T[]) binds to an IArrayTypeSymbol, so anything narrower than ITypeSymbol
                // would drop array registrations and make FN1008 unsilenceable for them.
                if (attribute.ConstructorArguments[0].Value is not ITypeSymbol registeredType)
                    continue;

                var qualifiedName = registeredType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                registeredTypeNames.Add(qualifiedName);
                registeredTypeNames.Add(qualifiedName.StartsWith("global::", StringComparison.Ordinal)
                    ? qualifiedName.Substring("global::".Length)
                    : "global::" + qualifiedName);
            }
        }

        registeredTypeNames.Sort(StringComparer.Ordinal);
        return registeredTypeNames;
    }

    // The source assembly plus only those references that use System.Text.Json at all: walking the
    // merged global namespace would realize every type in the whole reference closure.
    private static IEnumerable<INamedTypeSymbol> EnumerateCandidateTypes(Compilation compilation)
    {
        foreach (var namedType in EnumerateAllNamedTypes(compilation.Assembly.GlobalNamespace))
        {
            yield return namedType;
        }

        foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (!ReferencesSystemTextJson(referencedAssembly)) continue;
            foreach (var namedType in EnumerateAllNamedTypes(referencedAssembly.GlobalNamespace))
            {
                yield return namedType;
            }
        }
    }

    private static bool ReferencesSystemTextJson(IAssemblySymbol assemblySymbol)
    {
        foreach (var module in assemblySymbol.Modules)
        {
            foreach (var referencedAssemblyName in module.ReferencedAssemblies)
            {
                if (string.Equals(referencedAssemblyName.Name, SystemTextJsonAssemblyName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateAllNamedTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol nestedNamespace)
            {
                foreach (var innerType in EnumerateAllNamedTypes(nestedNamespace))
                {
                    yield return innerType;
                }
            }
            else if (member is INamedTypeSymbol namedType)
            {
                foreach (var typeOrNested in EnumerateWithNestedTypes(namedType))
                {
                    yield return typeOrNested;
                }
            }
        }
    }

    // Recursive rather than one level deep: a JsonSerializerContext nested two or more types down
    // would otherwise be invisible and produce a false-positive FN1008.
    private static IEnumerable<INamedTypeSymbol> EnumerateWithNestedTypes(INamedTypeSymbol namedType)
    {
        yield return namedType;
        foreach (var nestedType in namedType.GetTypeMembers())
        {
            foreach (var typeOrNested in EnumerateWithNestedTypes(nestedType))
            {
                yield return typeOrNested;
            }
        }
    }

    private static bool IsSubclassOf(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType)) return true;
            current = current.BaseType;
        }
        return false;
    }
}
