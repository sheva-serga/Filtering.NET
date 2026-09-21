using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// Reads every [MapNested] on a filter class and classifies its navigation against the entity symbol,
// so NestedFilterResolver can splice using strings alone and needs no Compilation of its own.
internal static class MapNestedExtractor
{
    private const string MapNestedAttributeFullName = "Filtering.Net.MapNestedAttribute";
    private const string MapNestedGenericMetadataName = "MapNestedAttribute`1";
    private const string FilteringNetNamespace = "Filtering.Net";

    public static EquatableList<NestedMappingModel> Extract(
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol entityType,
        List<DiagnosticInfo> diagnostics,
        CancellationToken cancellationToken)
    {
        var nestedMappings = new List<NestedMappingModel>();

        foreach (var attributeData in classSymbol.GetAttributes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attributeClass = attributeData.AttributeClass;
            if (attributeClass is null) continue;

            var matchesNonGeneric = attributeClass.ToDisplayString() == MapNestedAttributeFullName;
            var matchesGeneric = attributeClass.IsGenericType
                && attributeClass.OriginalDefinition.MetadataName == MapNestedGenericMetadataName
                && attributeClass.OriginalDefinition.ContainingNamespace?.ToDisplayString() == FilteringNetNamespace;

            if (!matchesNonGeneric && !matchesGeneric) continue;

            var attributeLocation = GetAttributeLocation(attributeData);

            var navigationPropertyName = attributeData.ConstructorArguments.Length >= 1
                ? attributeData.ConstructorArguments[0].Value as string ?? string.Empty
                : string.Empty;

            string? explicitFilterClassFqn = null;
            LocationInfo? explicitFilterClassLocation = null;
            if (matchesGeneric && attributeClass.TypeArguments.Length == 1)
            {
                var explicitFilterClass = attributeClass.TypeArguments[0];
                explicitFilterClassFqn = explicitFilterClass
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .Replace("global::", string.Empty);
                explicitFilterClassLocation = LocationInfo.FromLocation(explicitFilterClass.Locations.FirstOrDefault());
            }

            string? prefix = null;
            var prefixArgumentPresent = false;
            IReadOnlyList<string> only = Array.Empty<string>();
            IReadOnlyList<string> except = Array.Empty<string>();
            var hasOnly = false;
            var disableSorting = false;
            var maxDepth = 0;

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                switch (namedArgument.Key)
                {
                    case "Prefix":
                        prefixArgumentPresent = true;
                        prefix = namedArgument.Value.Value as string;
                        break;
                    case "Only":
                        if (!namedArgument.Value.IsNull)
                        {
                            only = ToStringList(namedArgument.Value.Values);
                            hasOnly = true;
                        }
                        break;
                    case "Except":
                        if (!namedArgument.Value.IsNull) except = ToStringList(namedArgument.Value.Values);
                        break;
                    case "DisableSorting":
                        disableSorting = namedArgument.Value.Value is bool disableSortingValue && disableSortingValue;
                        break;
                    case "MaxDepth":
                        if (namedArgument.Value.Value is int maxDepthValue) maxDepth = maxDepthValue;
                        break;
                }
            }

            // FN0025: a Prefix that was written but is blank would reach the runtime, which rejects it
            // at schema construction. Only an absent Prefix falls back to the navigation name.
            if (prefixArgumentPresent && string.IsNullOrWhiteSpace(prefix))
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.NestedPrefixBlank,
                    attributeLocation,
                    navigationPropertyName));
            }

            // Default Prefix is the CLR navigation name; downstream PropertyName becomes a verbatim CLR accessor path.
            var resolvedPrefix = string.IsNullOrWhiteSpace(prefix)
                ? navigationPropertyName
                : prefix!;

            // Deliberately no FN1006 here. A dotted [Map] over a nullable navigation can be rewritten
            // as a [PropertyMap] rule that null-guards, which is what FN1006 tells the consumer to do;
            // a [MapNested] cannot, because the lifted properties are declared on the nested filter
            // class. An optional reference navigation is the normal EF shape, so the warning would
            // fire on the supported case with no in-source remedy.
            var navigation = ClassifyNavigation(entityType, navigationPropertyName);

            nestedMappings.Add(new NestedMappingModel(
                NavigationPropertyName: navigationPropertyName,
                Prefix: resolvedPrefix,
                ExplicitFilterClassFqn: explicitFilterClassFqn,
                Only: new EquatableList<string>(only),
                Except: new EquatableList<string>(except),
                DisableSorting: disableSorting,
                AttributeLocation: LocationInfo.FromLocation(attributeLocation),
                HasOnly: hasOnly,
                NavigationKind: navigation.Kind,
                NavigationTypeFullName: navigation.TypeFullName,
                NavigationLocation: navigation.DeclarationLocation,
                ExplicitFilterClassLocation: explicitFilterClassLocation,
                MaxDepth: maxDepth));
        }

        return new EquatableList<NestedMappingModel>(nestedMappings);
    }

    private readonly struct NavigationClassification(
        NestedNavigationKind kind,
        string? typeFullName,
        LocationInfo? declarationLocation)
    {
        public NestedNavigationKind Kind { get; } = kind;

        public string? TypeFullName { get; } = typeFullName;

        public LocationInfo? DeclarationLocation { get; } = declarationLocation;
    }

    private static NavigationClassification ClassifyNavigation(INamedTypeSymbol entityType, string navigationPropertyName)
    {
        if (string.IsNullOrEmpty(navigationPropertyName))
        {
            return new NavigationClassification(NestedNavigationKind.Missing, null, null);
        }

        var navigationProperty = PropertyTypeResolver.FindProperty(entityType, navigationPropertyName);
        if (navigationProperty is null)
        {
            return new NavigationClassification(NestedNavigationKind.Missing, null, null);
        }

        var declarationLocation = LocationInfo.FromLocation(navigationProperty.Locations.FirstOrDefault());
        var typeFullName = navigationProperty.Type
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty);

        if (IsPrimitiveOrValueType(navigationProperty.Type))
        {
            return new NavigationClassification(NestedNavigationKind.PrimitiveOrValue, typeFullName, declarationLocation);
        }
        if (IsCollectionType(navigationProperty.Type))
        {
            return new NavigationClassification(NestedNavigationKind.Collection, typeFullName, declarationLocation);
        }
        return new NavigationClassification(NestedNavigationKind.Reference, typeFullName, declarationLocation);
    }

    // Leaf types (string, primitives, structs like DateTime/Guid) belong to [Map]/[PropertyMap], not [MapNested].
    private static bool IsPrimitiveOrValueType(ITypeSymbol type)
    {
        if (type.SpecialType != SpecialType.None) return true;
        if (type.IsValueType) return true;
        if (type.ContainingNamespace?.ToDisplayString() == "System") return true;
        return false;
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol) return true;
        if (type is not INamedTypeSymbol named) return false;
        // A navigation declared as IEnumerable<T> itself is a collection; its own AllInterfaces
        // only carries the non-generic IEnumerable, so the interface walk alone would miss it.
        if (named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T) return true;
        foreach (var implementedInterface in named.AllInterfaces)
        {
            if (implementedInterface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                return true;
            }
        }
        return false;
    }

    private static IReadOnlyList<string> ToStringList(System.Collections.Immutable.ImmutableArray<TypedConstant> values)
    {
        var result = new List<string>(values.Length);
        foreach (var typedConstant in values)
        {
            if (typedConstant.Value is string stringValue && !string.IsNullOrEmpty(stringValue))
            {
                result.Add(stringValue);
            }
        }
        return result;
    }

    private static Location? GetAttributeLocation(AttributeData attributeData) =>
        attributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation();
}
