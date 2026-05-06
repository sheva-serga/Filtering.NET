using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// Resolves dotted property paths (e.g., "Department.Name") on an entity type to the final IPropertySymbol.
internal static class PropertyTypeResolver
{
    public readonly struct ResolutionResult(IPropertySymbol? leafProperty, bool crossesNullableNavigation)
    {
        public IPropertySymbol? LeafProperty { get; } = leafProperty;

        // True when an intermediate segment is a nullable reference-type navigation (FN1006).
        public bool CrossesNullableNavigation { get; } = crossesNullableNavigation;
    }

    public static IPropertySymbol? Resolve(INamedTypeSymbol entityType, string path)
    {
        return ResolveWithNullableInfo(entityType, path).LeafProperty;
    }

    public static ResolutionResult ResolveWithNullableInfo(INamedTypeSymbol entityType, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new ResolutionResult(leafProperty: null, crossesNullableNavigation: false);
        }
        var segments = path.Split('.');
        ITypeSymbol currentType = entityType;
        IPropertySymbol? lastResolved = null;
        var crossesNullableNavigation = false;
        for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
        {
            var segmentName = segments[segmentIndex];
            if (currentType is not INamedTypeSymbol namedType)
            {
                return new ResolutionResult(leafProperty: null, crossesNullableNavigation: crossesNullableNavigation);
            }
            var property = FindProperty(namedType, segmentName);
            if (property is null)
            {
                return new ResolutionResult(leafProperty: null, crossesNullableNavigation: crossesNullableNavigation);
            }
            // Intermediate segments only — the leaf's own nullability is the value's nullability,
            // not a navigation hop, so we don't count it for FN1006.
            if (segmentIndex < segments.Length - 1 && IsNullableReferenceType(property.Type))
            {
                crossesNullableNavigation = true;
            }
            lastResolved = property;
            currentType = UnwrapNullable(property.Type);
        }
        return new ResolutionResult(lastResolved, crossesNullableNavigation);
    }

    private static IPropertySymbol? FindProperty(INamedTypeSymbol containingType, string name)
    {
        for (var currentType = (INamedTypeSymbol?)containingType; currentType is not null; currentType = currentType.BaseType)
        {
            foreach (var member in currentType.GetMembers(name))
            {
                if (member is IPropertySymbol propertySymbol) return propertySymbol;
            }
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

    private static bool IsNullableReferenceType(ITypeSymbol type)
    {
        return type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.Annotated;
    }
}
