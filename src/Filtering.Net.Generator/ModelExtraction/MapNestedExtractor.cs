using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

internal static class MapNestedExtractor
{
    private const string MapNestedAttributeFullName = "Filtering.Net.MapNestedAttribute";
    private const string MapNestedGenericMetadataName = "MapNestedAttribute`1";
    private const string FilteringNetNamespace = "Filtering.Net";

    public static EquatableList<NestedMappingModel> Extract(
        INamedTypeSymbol classSymbol,
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

            var navigationPropertyName = attributeData.ConstructorArguments.Length >= 1
                ? attributeData.ConstructorArguments[0].Value as string ?? string.Empty
                : string.Empty;

            string? explicitFilterClassFqn = null;
            if (matchesGeneric && attributeClass.TypeArguments.Length == 1)
            {
                explicitFilterClassFqn = attributeClass.TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .Replace("global::", string.Empty);
            }

            string? prefix = null;
            IReadOnlyList<string> only = Array.Empty<string>();
            IReadOnlyList<string> except = Array.Empty<string>();
            var disableSorting = false;
            var maxDepth = 0;

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                switch (namedArgument.Key)
                {
                    case "Prefix":
                        prefix = namedArgument.Value.Value as string;
                        break;
                    case "Only":
                        if (!namedArgument.Value.IsNull) only = ToStringList(namedArgument.Value.Values);
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

            // Default Prefix is the CLR navigation name; downstream PropertyName becomes a verbatim CLR accessor path.
            var resolvedPrefix = string.IsNullOrEmpty(prefix)
                ? navigationPropertyName
                : prefix!;

            nestedMappings.Add(new NestedMappingModel(
                NavigationPropertyName: navigationPropertyName,
                Prefix: resolvedPrefix,
                ExplicitFilterClassFqn: explicitFilterClassFqn,
                Only: new EquatableList<string>(only),
                Except: new EquatableList<string>(except),
                DisableSorting: disableSorting,
                AttributeLocation: LocationInfo.FromLocation(GetAttributeLocation(attributeData)),
                MaxDepth: maxDepth));
        }

        return new EquatableList<NestedMappingModel>(nestedMappings);
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
