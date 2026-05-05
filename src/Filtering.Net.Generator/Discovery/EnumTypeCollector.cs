using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

/// <summary>Scans every type in the compilation that carries
/// <c>[GenerateFilter&lt;TEntity&gt;]</c>; returns the set of distinct enum CLR types
/// referenced by any of their filterable properties. Drives auto-emission of per-enum
/// profile classes by <c>EnumProfileEmitter</c>.</summary>
internal static class EnumTypeCollector
{
    private const string GenerateFilterAttributeOpenName = "Filtering.Net.GenerateFilterAttribute<TEntity>";

    public static IReadOnlyList<INamedTypeSymbol> Collect(Compilation compilation)
    {
        var enums = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);

        foreach (var type in EnumerateAllTypes(compilation.GlobalNamespace))
        {
            INamedTypeSymbol? entityType = null;
            foreach (var attribute in type.GetAttributes())
            {
                var attrClass = attribute.AttributeClass;
                if (attrClass?.OriginalDefinition?.ToDisplayString() != GenerateFilterAttributeOpenName) continue;
                if (attrClass.TypeArguments.Length != 1) continue;
                if (attrClass.TypeArguments[0] is INamedTypeSymbol e) entityType = e;
                break;
            }
            if (entityType is null) continue;

            foreach (var member in entityType.GetMembers())
            {
                if (member is not IPropertySymbol property) continue;
                var unwrapped = UnwrapNullable(property.Type);
                if (unwrapped is INamedTypeSymbol enumNamed && enumNamed.TypeKind == TypeKind.Enum)
                {
                    enums[enumNamed.ToDisplayString()] = enumNamed;
                }
            }
        }

        return [.. enums.Values];
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol n && n.IsGenericType && n.ConstructedFrom?.SpecialType == SpecialType.System_Nullable_T)
            return n.TypeArguments[0];
        return type;
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateAllTypes(INamespaceSymbol root)
    {
        foreach (var member in root.GetMembers())
        {
            if (member is INamespaceSymbol ns)
                foreach (var t in EnumerateAllTypes(ns)) yield return t;
            else if (member is INamedTypeSymbol t)
            {
                yield return t;
                foreach (var nested in t.GetTypeMembers()) yield return nested;
            }
        }
    }
}
