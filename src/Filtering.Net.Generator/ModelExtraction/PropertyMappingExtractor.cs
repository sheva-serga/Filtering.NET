using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

/// <summary>
/// Extracts a single <see cref="PropertyMappingModel"/> from a <c>[Map]</c>-decorated method.
/// Validates the named arguments against the entity type and the resolved profile, emitting
/// the relevant FNxxxx diagnostics.
/// </summary>
internal static class PropertyMappingExtractor
{
    private const string ConvertWithAttributeMetadataName = "Filtering.Net.ConvertWithAttribute`1";
    private const string SortDirEnumFullName = "Filtering.Net.SortDir";

    /// <summary>
    /// Open-generic display string used to recognise an EF Core <c>ValueConverter&lt;TModel, TProvider&gt;</c>.
    /// We compare against this string so the analyzer does not need a hard reference to EF Core
    /// (it must remain EF-independent and ship from a netstandard2.0 generator project).
    /// </summary>
    private const string ValueConverterOpenGenericFullName =
        "Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<TModel, TProvider>";

    public static PropertyMappingExtractionResult Extract(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol entityType,
        AttributeData mapAttribute,
        AttributeData? convertWithAttribute,
        Compilation compilation,
        ProfileIndex profileIndex)
    {
        var diagnostics = new List<DiagnosticInfo>();

        var mapLocation = methodSymbol.Locations.FirstOrDefault();

        // -------- Constructor arg: PropertyName --------
        var propertyName = ReadConstructorString(mapAttribute, position: 0);
        if (string.IsNullOrEmpty(propertyName))
        {
            // Without a property name we can't even attribute downstream diagnostics.
            return new PropertyMappingExtractionResult(Model: null, Diagnostics: diagnostics);
        }

        // -------- Resolve property on entity --------
        var resolution = PropertyTypeResolver.ResolveWithNullableInfo(entityType, propertyName!);
        var propertySymbol = resolution.LeafProperty;
        if (propertySymbol is null)
        {
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.PropertyNotFound,
                mapLocation,
                propertyName!,
                entityType.ToDisplayString()));
            return new PropertyMappingExtractionResult(Model: null, Diagnostics: diagnostics);
        }

        // FN1006: path crossed a nullable navigation (e.g., Department?.Name on a non-nullable
        // string — the intermediate Department? could be null at evaluation time, the generated
        // SQL needs explicit null handling otherwise EF emits unintended LEFT JOIN semantics).
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
        // Symbol of the resolved profile, when available; used to walk the BasedOn chain to
        // find the extractor-owning ancestor. Null for the auto-emitted enum profile path
        // (the symbol doesn't yet exist) and for the legacy InferFromClrType fallback.
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
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.IncompatibleProfile,
                    mapLocation,
                    resolvedProfile.ProfileFullName,
                    propertyName!,
                    propertyClrType));
            }
        }
        else
        {
            // Index-first resolution: scan [FilterProfile<T>]-annotated profile classes for a
            // match against this property's CLR type. Multiple matches → FN0015 (ambiguous);
            // single match → re-run through ResolveExplicit so we pick up the profile's actual
            // [FilterOperator] list (relevant for custom profiles registering a primitive type).
            // Zero matches OR symbol resolution failure → fall back to the legacy
            // InferFromClrType switch so existing snapshot tests stay green during the
            // transition. Task 21 deletes the legacy fallback once every profile resolves
            // through the index.
            var candidates = ProfileResolver.ResolveCandidates(propertySymbol.Type, profileIndex);
            if (candidates.Count > 1)
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.AmbiguousProfile,
                    mapLocation,
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
                    // Auto-emitted enum profiles live under Filtering.Net.Generated and are
                    // produced by this same generator pass — they aren't visible to
                    // GetTypeByMetadataName at index-resolution time. The profile shape is
                    // fixed (eq/ne/in/isNull) so we can synthesise a ResolvedProfile from
                    // just the candidate's full name. The synthetic profile owns its own
                    // extractor so we don't need a symbol to walk BasedOn.
                    resolvedProfile = ProfileResolver.TryBuildVirtualEnumProfile(
                        candidateProfileFullName, propertySymbol.Type);
                }
            }

            if (resolvedProfile is null)
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.NoInferableProfile,
                    mapLocation,
                    propertyName!,
                    propertyClrType));
                return new PropertyMappingExtractionResult(Model: null, Diagnostics: diagnostics);
            }
        }

        // Compute the extractor profile name. Built-ins / auto-emitted enum profiles own
        // their own TryGet helpers; user-defined custom profiles delegate to a BasedOn root.
        // When we have no symbol (synthetic enum profile or legacy InferFromClrType fallback),
        // the resolved profile already names an extractor-owning class.
        var extractorProfileFullName = resolvedProfileSymbol is not null
            ? ProfileResolver.ResolveExtractorProfileFullName(resolvedProfileSymbol)
            : resolvedProfile.ProfileFullName;

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

        // -------- ConvertWith handling --------
        string? valueConverterFullName = null;
        string? converterModelClrType = null;
        if (convertWithAttribute is not null)
        {
            var attributeClass = convertWithAttribute.AttributeClass;
            if (attributeClass is { IsGenericType: true } && attributeClass.TypeArguments.Length == 1)
            {
                if (attributeClass.TypeArguments[0] is INamedTypeSymbol converterType)
                {
                    valueConverterFullName = converterType.ToDisplayString();
                    var modelType = TryFindValueConverterModelType(converterType);
                    if (modelType is not null)
                    {
                        // TModel from ValueConverter<TModel, TProvider> becomes the value
                        // parameter's CLR type at every layer (validation deserialization,
                        // typed leaf methods). EF translates the converter at SQL-emit time so
                        // the emitter never inserts a conversion call itself.
                        // We store the bare display string (no global:: prefix) for symmetry
                        // with PropertyClrType — PropertyValueShapeResolver does its own
                        // qualification when emitting.
                        converterModelClrType = modelType.ToDisplayString();
                    }
                    // FN0007: walk the converter's base chain. If it never inherits from
                    // ValueConverter<TModel, TProvider> (matched on open-generic display string,
                    // so the analyzer doesn't depend on EF Core), emit FN0007.
                    if (!InheritsFromValueConverter(converterType))
                    {
                        diagnostics.Add(DiagnosticInfo.From(
                            DiagnosticDescriptors.InvalidValueConverter,
                            mapLocation,
                            valueConverterFullName));
                    }
                }
            }
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

        // Filter the resolved profile's custom-operator metadata down to the operators that
        // survived Only/Except — there's no point carrying metadata for operators the leaf
        // dispatcher will never invoke, and skipping keeps the snapshot output stable.
        var allowedSet = new HashSet<string>(allowedOperators, StringComparer.Ordinal);
        var filteredCustomOperators = resolvedProfile.CustomOperators
            .Where(customOperator => allowedSet.Contains(customOperator.OperatorName))
            .ToList();

        // A property has a typed-value operator when at least one of its surviving custom
        // operators carries a non-null ValueClrType (i.e., the operator's lambda takes a typed
        // value parameter). Built-in profile operators always go through TryGetValue /
        // TryGetArray (JsonElement-based extraction) and never produce CustomOperatorModel
        // entries, so they can never set this flag. Unary custom operators have
        // ValueClrType == null and likewise leave the flag false.
        var hasTypedValueOperator = filteredCustomOperators.Exists(customOperator => customOperator.ValueClrType is not null);

        var model = new PropertyMappingModel(
            PropertyName: propertyName!,
            PropertyClrType: propertyClrType,
            ProfileFullName: resolvedProfile.ProfileFullName,
            ExtractorProfileFullName: extractorProfileFullName,
            AllowedOperators: new EquatableList<string>(allowedOperators),
            Alias: alias,
            Sortable: sortable,
            DefaultSortDirection: defaultSortDirection,
            ValueConverterFullName: valueConverterFullName,
            ConverterModelClrType: converterModelClrType,
            ConfigurationMethodName: methodSymbol.Name,
            CustomOperators: new EquatableList<CustomOperatorModel>(filteredCustomOperators),
            HasTypedValueOperator: hasTypedValueOperator);

        return new PropertyMappingExtractionResult(Model: model, Diagnostics: diagnostics);
    }

    /// <summary>
    /// True when <paramref name="converterType"/> (or any base type) is a constructed
    /// <c>Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter&lt;TModel, TProvider&gt;</c>.
    /// We compare on <see cref="INamedTypeSymbol.OriginalDefinition"/> by display string so we
    /// don't take a hard reference on the EF Core assembly.
    /// </summary>
    private static bool InheritsFromValueConverter(INamedTypeSymbol converterType)
    {
        for (INamedTypeSymbol? currentType = converterType; currentType is not null; currentType = currentType.BaseType)
        {
            var openGenericDisplay = currentType.OriginalDefinition.ToDisplayString();
            if (openGenericDisplay == ValueConverterOpenGenericFullName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Walks the converter type's base chain to find the constructed
    /// <c>ValueConverter&lt;TModel, TProvider&gt;</c> base and returns <c>TModel</c>. Returns
    /// null when the chain doesn't contain such a base (FN0007 will already have fired in that
    /// case). Matched on the open-generic display string so the analyzer remains EF-independent.
    /// </summary>
    private static ITypeSymbol? TryFindValueConverterModelType(INamedTypeSymbol converterType)
    {
        for (INamedTypeSymbol? currentType = converterType; currentType is not null; currentType = currentType.BaseType)
        {
            var openGenericDisplay = currentType.OriginalDefinition.ToDisplayString();
            if (openGenericDisplay != ValueConverterOpenGenericFullName) continue;
            if (currentType.TypeArguments.Length < 1) return null;
            return currentType.TypeArguments[0];
        }
        return null;
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
            // SortDir is defined as Asc=0, Desc=1 in the runtime project.
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
