using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

internal static class PropertyMappingExtractor
{
    private const string SortDirEnumFullName = "Filtering.Net.SortDir";

    public static PropertyMappingExtractionResult Extract(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol entityType,
        AttributeData mapAttribute,
        Compilation compilation,
        ProfileIndex profileIndex)
    {
        var diagnostics = new List<DiagnosticInfo>();

        var mapLocation = methodSymbol.Locations.FirstOrDefault();

        // -------- Constructor arg: PropertyName --------
        var propertyName = ReadConstructorString(mapAttribute, position: 0);
        if (string.IsNullOrEmpty(propertyName))
        {
            return new PropertyMappingExtractionResult(Model: null, Diagnostics: diagnostics);
        }

        // -------- Resolve property on entity --------
        var resolution = PropertyTypeResolver.ResolveWithNullableInfo(entityType, propertyName!);
        var propertySymbol = resolution.LeafProperty;
        if (propertySymbol is null)
        {
            var entityLocation = entityType.Locations.FirstOrDefault();
            var additionalLocations = entityLocation is not null
                ? new[] { entityLocation }
                : Array.Empty<Location>();
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.PropertyNotFound,
                mapLocation,
                additionalLocations,
                propertyName!,
                entityType.ToDisplayString()));
            return new PropertyMappingExtractionResult(Model: null, Diagnostics: diagnostics);
        }

        // FN1006: intermediate navigation is nullable — EF may produce unintended LEFT JOIN semantics.
        if (resolution.CrossesNullableNavigation)
        {
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NullableNavInPath,
                mapLocation,
                propertyName!));
        }

        var propertyClrType = propertySymbol.Type.ToDisplayString();

        // -------- Named args --------
        INamedTypeSymbol? explicitProfile = null;
        ImmutableArray<TypedConstant> onlyOperators = default;
        ImmutableArray<TypedConstant> exceptOperators = default;
        string? alias = null;
        var sortable = false;
        var defaultSortDirection = "Asc";

        foreach (var namedArgument in mapAttribute.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case "Profile":
                    explicitProfile = namedArgument.Value.Value as INamedTypeSymbol;
                    break;
                case "Only":
                    if (!namedArgument.Value.IsNull) onlyOperators = namedArgument.Value.Values;
                    break;
                case "Except":
                    if (!namedArgument.Value.IsNull) exceptOperators = namedArgument.Value.Values;
                    break;
                case "Alias":
                    alias = namedArgument.Value.Value as string;
                    break;
                case "Sortable":
                    sortable = namedArgument.Value.Value is bool sortableValue && sortableValue;
                    break;
                case "DefaultSortDirection":
                    defaultSortDirection = ResolveEnumName(namedArgument.Value, defaultSortDirection);
                    break;
            }
        }

        // -------- Resolve profile --------
        ResolvedProfile? resolvedProfile;
        // Null for auto-emitted enum profiles (not yet visible to GetTypeByMetadataName).
        INamedTypeSymbol? resolvedProfileSymbol = null;
        if (explicitProfile is not null)
        {
            resolvedProfile = ProfileResolver.ResolveExplicit(explicitProfile, compilation);
            if (resolvedProfile is null)
            {
                // Explicit profile didn't resolve (no [FilterOperator] members). Bail out cleanly.
                return new PropertyMappingExtractionResult(Model: null, Diagnostics: diagnostics);
            }
            resolvedProfileSymbol = explicitProfile;
            if (!ProfileResolver.IsCompatible(propertySymbol.Type, resolvedProfile.ProfileFullName))
            {
                var profileLocation = explicitProfile.Locations.FirstOrDefault();
                var additionalLocations = profileLocation is not null
                    ? new[] { profileLocation }
                    : Array.Empty<Location>();
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.IncompatibleProfile,
                    mapLocation,
                    additionalLocations,
                    resolvedProfile.ProfileFullName,
                    propertyName!,
                    propertyClrType));
            }
        }
        else
        {
            var candidates = ProfileResolver.ResolveCandidates(propertySymbol.Type, profileIndex);
            if (candidates.Count > 1)
            {
                var candidateLocations = new List<Location>(candidates.Count);
                foreach (var candidateFullName in candidates.ProfileFullNames)
                {
                    var candidateSymbol = compilation.GetTypeByMetadataName(candidateFullName);
                    var candidateLocation = candidateSymbol?.Locations.FirstOrDefault();
                    if (candidateLocation is not null)
                    {
                        candidateLocations.Add(candidateLocation);
                    }
                }
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.AmbiguousProfile,
                    mapLocation,
                    candidateLocations.ToArray(),
                    propertyName!,
                    propertyClrType,
                    string.Join(", ", candidates.ProfileFullNames)));
                return new PropertyMappingExtractionResult(Model: null, Diagnostics: diagnostics);
            }

            resolvedProfile = null;
            if (candidates.Count == 1)
            {
                var candidateProfileFullName = candidates.ProfileFullNames[0];
                var profileSymbol = compilation.GetTypeByMetadataName(candidateProfileFullName);
                if (profileSymbol is not null)
                {
                    resolvedProfile = ProfileResolver.ResolveExplicit(profileSymbol, compilation);
                    resolvedProfileSymbol = profileSymbol;
                }
                else
                {
                    // Auto-emitted enum profiles are produced by this same generator pass and
                    // aren't yet visible to GetTypeByMetadataName; synthesise from the full name.
                    resolvedProfile = ProfileResolver.TryBuildVirtualEnumProfile(
                        candidateProfileFullName, propertySymbol.Type);
                }
            }

            if (resolvedProfile is null)
            {
                var entityPropertyLocation = propertySymbol.Locations.FirstOrDefault();
                var additionalLocations = entityPropertyLocation is not null
                    ? new[] { entityPropertyLocation }
                    : Array.Empty<Location>();
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.NoInferableProfile,
                    mapLocation,
                    additionalLocations,
                    propertyName!,
                    propertyClrType));
                return new PropertyMappingExtractionResult(Model: null, Diagnostics: diagnostics);
            }
        }

        // No symbol means an auto-emitted enum profile, which carries its own runtime Profile accessor.
        var profileBridges = resolvedProfileSymbol is not null
            ? ProfileBridgeBuilder.BuildChain(resolvedProfileSymbol)
            : [];

        // -------- Compute allowed operators --------
        var profileOperators = resolvedProfile.Operators;
        var profileOperatorSet = new HashSet<string>(profileOperators);

        var onlySet = ToStringSet(onlyOperators);
        var exceptSet = ToStringSet(exceptOperators);

        // FN0006: any name in Only/Except that isn't on the profile is an error.
        if (onlySet is not null)
        {
            foreach (var operatorName in onlySet)
            {
                if (!profileOperatorSet.Contains(operatorName))
                {
                    diagnostics.Add(DiagnosticInfo.From(
                        DiagnosticDescriptors.UnknownOperator,
                        mapLocation,
                        operatorName,
                        resolvedProfile.ProfileFullName));
                }
            }
        }
        if (exceptSet is not null)
        {
            foreach (var operatorName in exceptSet)
            {
                if (!profileOperatorSet.Contains(operatorName))
                {
                    diagnostics.Add(DiagnosticInfo.From(
                        DiagnosticDescriptors.UnknownOperator,
                        mapLocation,
                        operatorName,
                        resolvedProfile.ProfileFullName));
                }
            }
        }

        var allowedOperators = new List<string>();
        foreach (var operatorName in profileOperators)
        {
            if (onlySet is not null && !onlySet.Contains(operatorName)) continue;
            if (exceptSet is not null && exceptSet.Contains(operatorName)) continue;
            allowedOperators.Add(operatorName);
        }

        // FN1005: Only/Except resolved to an empty allowed-operator set. The property would
        // never accept any leaf, so anything that lands on it will fail validation at runtime.
        if (allowedOperators.Count == 0)
        {
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.ZeroOperatorsAllowed,
                mapLocation,
                propertyName!));
        }

        // Drop metadata for operators excluded by Only/Except — the dispatcher will never invoke them.
        var allowedSet = new HashSet<string>(allowedOperators, StringComparer.Ordinal);
        var filteredCustomOperators = resolvedProfile.CustomOperators
            .Where(customOperator => allowedSet.Contains(customOperator.OperatorName))
            .ToList();

        var hasTypedValueOperator = filteredCustomOperators.Exists(customOperator => customOperator.ValueClrType is not null);

        var declarationLocation = LocationInfo.FromLocation(mapAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation());

        var model = new PropertyMappingModel(
            PropertyName: propertyName!,
            PropertyClrType: propertyClrType,
            IsNullableValueType: propertySymbol.Type is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T },
            ProfileFullName: resolvedProfile.ProfileFullName,
            AllowedOperators: new EquatableList<string>(allowedOperators),
            HasOperatorRestriction: onlySet is not null || exceptSet is not null,
            Alias: alias,
            Sortable: sortable,
            DefaultSortDirection: defaultSortDirection,
            ConfigurationMethodName: methodSymbol.Name,
            CustomOperators: new EquatableList<CustomOperatorModel>(filteredCustomOperators),
            HasTypedValueOperator: hasTypedValueOperator,
            ProfileBridges: new EquatableList<ProfileBridgeModel>(profileBridges),
            DeclarationLocation: declarationLocation);

        return new PropertyMappingExtractionResult(Model: model, Diagnostics: diagnostics);
    }

    private static string? ReadConstructorString(AttributeData attributeData, int position)
    {
        if (attributeData.ConstructorArguments.Length <= position) return null;
        return attributeData.ConstructorArguments[position].Value as string;
    }

    private static HashSet<string>? ToStringSet(ImmutableArray<TypedConstant> array)
    {
        if (array.IsDefault) return null;
        var result = new HashSet<string>();
        foreach (var typedConstant in array)
        {
            if (typedConstant.Value is string operatorName) result.Add(operatorName);
        }
        return result;
    }

    private static string ResolveEnumName(TypedConstant typedConstant, string fallback)
    {
        if (typedConstant.Type is INamedTypeSymbol enumType
            && enumType.ToDisplayString() == SortDirEnumFullName
            && typedConstant.Value is int enumValue)
        {
            return enumValue switch
            {
                0 => "Asc",
                1 => "Desc",
                _ => fallback,
            };
        }
        return fallback;
    }
}
