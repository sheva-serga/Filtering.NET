using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

internal static class EnumTypeCollector
{
    private const string GenerateFilterAttributeOpenName = "Filtering.Net.GenerateFilterAttribute<TEntity>";

    // Only the source assembly can declare the [GenerateFilter] partials this generator emits for,
    // so the walk stays out of the reference closure.
    public static IReadOnlyList<INamedTypeSymbol> Collect(Compilation compilation)
    {
        var enums = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);

        foreach (var type in SymbolEnumerator.EnumerateTypes(compilation.Assembly.GlobalNamespace))
        {
            var entityType = TryReadFilteredEntityType(type);
            if (entityType is null) continue;

            foreach (var member in entityType.GetMembers())
            {
                if (member is not IPropertySymbol property) continue;
                var unwrapped = UnwrapNullable(property.Type);
                if (unwrapped is INamedTypeSymbol enumType && enumType.TypeKind == TypeKind.Enum)
                {
                    enums[enumType.ToDisplayString()] = enumType;
                }
            }
        }

        return [.. enums.Values];
    }

    private static INamedTypeSymbol? TryReadFilteredEntityType(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass?.OriginalDefinition?.ToDisplayString() != GenerateFilterAttributeOpenName) continue;
            if (attributeClass.TypeArguments.Length != 1) continue;
            return attributeClass.TypeArguments[0] as INamedTypeSymbol;
        }
        return null;
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType
            && namedType.IsGenericType
            && namedType.ConstructedFrom?.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedType.TypeArguments[0];
        }
        return type;
    }
}
