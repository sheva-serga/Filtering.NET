using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// Two halves of one [Map]. ExtractDeclaration reads what the attribute and the entity symbol say and
// runs inside the syntax transform; ResolveMapping turns that into the emitted mapping once the
// compilation-wide GeneratorIndex is combined in. Splitting them keeps the transform free of
// compilation-global state that Roslyn does not track per syntax tree.
internal static class PropertyMappingExtractor
{
    private const string SortDirEnumFullName = "Filtering.Net.SortDir";

    public static PropertyDeclarationExtractionResult ExtractDeclaration(
        INamedTypeSymbol entityType,
        AttributeData mapAttribute)
    {
        var diagnostics = new List<DiagnosticInfo>();

        var mapLocation = mapAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();

        // -------- Constructor arg: PropertyName --------
        var propertyName = ReadConstructorString(mapAttribute, position: 0);
        if (string.IsNullOrEmpty(propertyName))
        {
            return new PropertyDeclarationExtractionResult(Model: null, Diagnostics: diagnostics);
        }

        // -------- Resolve property on entity --------
        var resolution = PropertyTypeResolver.ResolveWithNullableInfo(entityType, propertyName!);

        // FN0024: the emitted accessor would read the next segment off a Nullable<T>, which has no such member.
        if (resolution.NullableValueTypeSegment is not null)
        {
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NullableValueTypeInPath,
                mapLocation,
                propertyName!,
                resolution.NullableValueTypeSegment,
                resolution.NullableValueTypeName ?? string.Empty));
            return new PropertyDeclarationExtractionResult(Model: null, Diagnostics: diagnostics);
        }

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
            return new PropertyDeclarationExtractionResult(Model: null, Diagnostics: diagnostics);
        }

        // FN1006: intermediate navigation is nullable — EF may produce unintended LEFT JOIN semantics.
        if (resolution.CrossesNullableNavigation)
        {
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NullableNavInPath,
                mapLocation,
                propertyName!));
        }

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

        var declarationLocation = LocationInfo.FromLocation(mapLocation);

        var model = new PropertyDeclarationModel(
            PropertyName: propertyName!,
            PropertyClrType: propertySymbol.Type.ToDisplayString(),
            PropertyClrTypeKey: UnwrapNullable(propertySymbol.Type).ToDisplayString(),
            IsNullableValueType: propertySymbol.Type is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T },
            ExplicitProfileFullName: explicitProfile?.ToDisplayString(),
            ExplicitProfileLocation: LocationInfo.FromLocation(explicitProfile?.Locations.FirstOrDefault()),
            OnlyOperators: ToStringList(onlyOperators),
            HasOnly: !onlyOperators.IsDefault,
            ExceptOperators: ToStringList(exceptOperators),
            HasExcept: !exceptOperators.IsDefault,
            Alias: alias,
            Sortable: sortable,
            DefaultSortDirection: defaultSortDirection,
            DeclarationLocation: declarationLocation,
            LeafPropertyLocation: LocationInfo.FromLocation(propertySymbol.Locations.FirstOrDefault()));

        return new PropertyDeclarationExtractionResult(Model: model, Diagnostics: diagnostics);
    }

    public static PropertyMappingExtractionResult ResolveMapping(
        PropertyDeclarationModel declaration,
        GeneratorIndex index)
    {
        var diagnostics = new List<DiagnosticInfo>();

        ProfileDefinition? resolvedProfile;
        if (declaration.ExplicitProfileFullName is { } explicitProfileFullName)
        {
            resolvedProfile = index.FindProfile(explicitProfileFullName);

            // FN0022: Profile = typeof(X) where X carries no [FilterProfile<TColumn>]. Emission would
            // otherwise reference a bridge class that is never generated.
            if (resolvedProfile is null)
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.ProfileTypeNotAProfile,
                    declaration.DeclarationLocation,
                    new[] { declaration.ExplicitProfileLocation },
                    declaration.PropertyName,
                    explicitProfileFullName));
                return new PropertyMappingExtractionResult(Model: null, Diagnostics: diagnostics);
            }

            if (!ProfileResolver.IsCompatible(declaration.PropertyClrTypeKey, resolvedProfile.ProfileFullName))
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.IncompatibleProfile,
                    declaration.DeclarationLocation,
                    new[] { declaration.ExplicitProfileLocation },
                    resolvedProfile.ProfileFullName,
                    declaration.PropertyName,
                    declaration.PropertyClrType));
            }
        }
        else
        {
            var candidates = index.ProfilesByClrType.Lookup(declaration.PropertyClrTypeKey);
            if (candidates.Count > 1)
            {
                var candidateLocations = new List<LocationInfo?>(candidates.Count);
                foreach (var candidateFullName in candidates)
                {
                    candidateLocations.Add(index.FindProfile(candidateFullName)?.DeclarationLocation);
                }
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.AmbiguousProfile,
                    declaration.DeclarationLocation,
                    candidateLocations,
                    declaration.PropertyName,
                    declaration.PropertyClrType,
                    string.Join(", ", candidates)));
                return new PropertyMappingExtractionResult(Model: null, Diagnostics: diagnostics);
            }

            resolvedProfile = candidates.Count == 1 ? index.FindProfile(candidates[0]) : null;
            if (resolvedProfile is null)
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.NoInferableProfile,
                    declaration.DeclarationLocation,
                    new[] { declaration.LeafPropertyLocation },
                    declaration.PropertyName,
                    declaration.PropertyClrType));
                return new PropertyMappingExtractionResult(Model: null, Diagnostics: diagnostics);
            }
        }

        // -------- Compute allowed operators --------
        var profileOperatorSet = new HashSet<string>(resolvedProfile.Operators);

        var onlySet = declaration.HasOnly ? new HashSet<string>(declaration.OnlyOperators) : null;
        var exceptSet = declaration.HasExcept ? new HashSet<string>(declaration.ExceptOperators) : null;

        // FN0005: any name in Only/Except that isn't on the profile is an error.
        ReportUnknownOperators(onlySet, profileOperatorSet, declaration, resolvedProfile, diagnostics);
        ReportUnknownOperators(exceptSet, profileOperatorSet, declaration, resolvedProfile, diagnostics);

        var allowedOperators = new List<string>();
        foreach (var operatorName in resolvedProfile.Operators)
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
                declaration.DeclarationLocation,
                declaration.PropertyName));
        }

        // Drop metadata for operators excluded by Only/Except — the dispatcher will never invoke them.
        var allowedSet = new HashSet<string>(allowedOperators, StringComparer.Ordinal);
        var filteredCustomOperators = resolvedProfile.CustomOperators
            .Where(customOperator => allowedSet.Contains(customOperator.OperatorName))
            .ToList();

        var hasTypedValueOperator = filteredCustomOperators.Exists(customOperator => customOperator.ValueClrType is not null);

        var model = new PropertyMappingModel(
            PropertyName: declaration.PropertyName,
            PropertyClrType: declaration.PropertyClrType,
            IsNullableValueType: declaration.IsNullableValueType,
            ProfileFullName: resolvedProfile.ProfileFullName,
            AllowedOperators: new EquatableList<string>(allowedOperators),
            HasOperatorRestriction: onlySet is not null || exceptSet is not null,
            Alias: declaration.Alias,
            Sortable: declaration.Sortable,
            DefaultSortDirection: declaration.DefaultSortDirection,
            DeclarationName: declaration.PropertyName,
            CustomOperators: new EquatableList<CustomOperatorModel>(filteredCustomOperators),
            HasTypedValueOperator: hasTypedValueOperator,
            ProfileBridges: resolvedProfile.Bridges,
            DeclarationLocation: declaration.DeclarationLocation);

        return new PropertyMappingExtractionResult(Model: model, Diagnostics: diagnostics);
    }

    private static void ReportUnknownOperators(
        HashSet<string>? requestedOperators,
        HashSet<string> profileOperatorSet,
        PropertyDeclarationModel declaration,
        ProfileDefinition resolvedProfile,
        List<DiagnosticInfo> diagnostics)
    {
        if (requestedOperators is null) return;
        foreach (var operatorName in requestedOperators)
        {
            if (profileOperatorSet.Contains(operatorName)) continue;
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.UnknownOperator,
                declaration.DeclarationLocation,
                operatorName,
                resolvedProfile.ProfileFullName));
        }
    }

    private static string? ReadConstructorString(AttributeData attributeData, int position)
    {
        if (attributeData.ConstructorArguments.Length <= position) return null;
        return attributeData.ConstructorArguments[position].Value as string;
    }

    private static EquatableList<string> ToStringList(ImmutableArray<TypedConstant> array)
    {
        var result = new List<string>();
        if (array.IsDefault) return new EquatableList<string>(result);
        foreach (var typedConstant in array)
        {
            if (typedConstant.Value is string operatorName) result.Add(operatorName);
        }
        return new EquatableList<string>(result);
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
