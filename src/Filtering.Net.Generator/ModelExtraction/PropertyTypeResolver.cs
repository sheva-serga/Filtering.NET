using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// Resolves dotted property paths (e.g., "Department.Name") on an entity type to the final IPropertySymbol.
internal static class PropertyTypeResolver
{
    public readonly struct ResolutionResult(
        IPropertySymbol? leafProperty,
        bool crossesNullableNavigation,
        string? nullableValueTypeSegment = null,
        string? nullableValueTypeName = null)
    {
        public IPropertySymbol? LeafProperty { get; } = leafProperty;

        // True when an intermediate segment is a nullable reference-type navigation (FN1006).
        public bool CrossesNullableNavigation { get; } = crossesNullableNavigation;

        // Set when an intermediate segment is a Nullable<T>: the emitted accessor would read the
        // next member straight off the Nullable<T>, which does not expose it (FN0024).
        public string? NullableValueTypeSegment { get; } = nullableValueTypeSegment;

        public string? NullableValueTypeName { get; } = nullableValueTypeName;
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
            if (segmentIndex < segments.Length - 1)
            {
                // Intermediate segments only — the leaf's own nullability is the value's nullability,
                // not a navigation hop, so we don't count it for FN1006.
                if (IsNullableReferenceType(property.Type))
                {
                    crossesNullableNavigation = true;
                }
                if (IsNullableValueType(property.Type))
                {
                    return new ResolutionResult(
                        leafProperty: null,
                        crossesNullableNavigation: crossesNullableNavigation,
                        nullableValueTypeSegment: segments[segmentIndex + 1],
                        nullableValueTypeName: property.Type.ToDisplayString());
                }
            }
            lastResolved = property;
            currentType = UnwrapNullable(property.Type);
        }
        return new ResolutionResult(lastResolved, crossesNullableNavigation);
    }

    private static bool IsNullableValueType(ITypeSymbol type) =>
        type is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T };

    // Walks the base chain so a navigation declared on a shared base entity resolves the same way
    // for [MapNested] as a dotted [Map] path already does.
    public static IPropertySymbol? FindProperty(INamedTypeSymbol containingType, string name)
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
