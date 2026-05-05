using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

/// <summary>
/// Resolves dotted property paths (e.g., <c>"Department.Name"</c>) on an entity type to the
/// final <see cref="IPropertySymbol"/>.
/// </summary>
internal static class PropertyTypeResolver
{
    /// <summary>Result of a path resolution: the leaf property and a flag telling whether any intermediate segment is a nullable reference-type navigation.</summary>
    public readonly struct ResolutionResult(IPropertySymbol? leafProperty, bool crossesNullableNavigation)
    {
        public IPropertySymbol? LeafProperty { get; } = leafProperty;

        /// <summary>True when one of the intermediate segments was a nullable reference-typed navigation property (e.g., <c>Department?.Name</c>).</summary>
        public bool CrossesNullableNavigation { get; } = crossesNullableNavigation;
    }

    /// <summary>Convenience overload — returns just the leaf property; preserves the original API used elsewhere.</summary>
    public static IPropertySymbol? Resolve(INamedTypeSymbol entityType, string path)
    {
        return ResolveWithNullableInfo(entityType, path).LeafProperty;
    }

    /// <summary>Walks a dotted path on <paramref name="entityType"/>, returning the leaf property and whether any intermediate hop crossed a nullable navigation.</summary>
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
        // Walk the inheritance chain so inherited properties are visible too.
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

    /// <summary>True for reference types annotated as nullable (e.g., <c>Department?</c>). Excludes nullable value types — <c>int?</c> etc. don't carry navigation semantics.</summary>
    private static bool IsNullableReferenceType(ITypeSymbol type)
    {
        return type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.Annotated;
    }
}
