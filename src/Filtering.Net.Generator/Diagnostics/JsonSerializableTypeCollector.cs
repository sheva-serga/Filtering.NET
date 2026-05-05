using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

/// <summary>
/// Walks every <c>JsonSerializerContext</c> subclass visible in the compilation
/// and returns the union of types registered via <c>[JsonSerializable(typeof(T))]</c>.
/// Used by the FN1008 analyzer pass to determine which typed-value types are already
/// covered for AOT-safe deserialization.
/// </summary>
internal static class JsonSerializableTypeCollector
{
    private const string JsonSerializableAttributeFullName = "System.Text.Json.Serialization.JsonSerializableAttribute";
    private const string JsonSerializerContextFullName = "System.Text.Json.Serialization.JsonSerializerContext";

    /// <summary>
    /// Returns the set of types registered with <c>[JsonSerializable(typeof(T))]</c>
    /// on any <c>JsonSerializerContext</c> subclass visible in the compilation.
    /// Returns an empty set when <c>System.Text.Json</c> is not referenced.
    /// </summary>
    public static HashSet<INamedTypeSymbol> CollectRegisteredTypes(Compilation compilation)
    {
        var registeredTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        var jsonSerializerContextSymbol = compilation.GetTypeByMetadataName(JsonSerializerContextFullName);
        if (jsonSerializerContextSymbol is null)
        {
            return registeredTypes; // System.Text.Json not referenced — nothing to register.
        }

        var jsonSerializableAttributeSymbol = compilation.GetTypeByMetadataName(JsonSerializableAttributeFullName);
        if (jsonSerializableAttributeSymbol is null)
        {
            return registeredTypes;
        }

        foreach (var namedType in EnumerateAllNamedTypes(compilation.GlobalNamespace))
        {
            if (!IsSubclassOf(namedType, jsonSerializerContextSymbol)) continue;

            foreach (var attribute in namedType.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, jsonSerializableAttributeSymbol))
                    continue;
                if (attribute.ConstructorArguments.Length == 0)
                    continue;
                if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol registeredType)
                {
                    registeredTypes.Add(registeredType);
                }
            }
        }

        return registeredTypes;
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
                yield return namedType;
                foreach (var nestedType in namedType.GetTypeMembers())
                {
                    yield return nestedType;
                }
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
