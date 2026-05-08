using System.Collections.Immutable;
using System.Globalization;

using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

internal static class NestedFilterResolver
{
    public sealed record ResolvedHost(
        FilterClassModel Model,
        EquatableList<DiagnosticInfo> Diagnostics);

    public static ResolvedHost Resolve(
        FilterClassModelWithDiagnostics hostExtractionResult,
        ImmutableArray<FilterClassModelWithDiagnostics> allHostExtractionResults,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        if (hostExtractionResult.Model is null)
        {
            return new ResolvedHost(hostExtractionResult.Model!, new EquatableList<DiagnosticInfo>());
        }

        var hostModel = hostExtractionResult.Model;
        var newDiagnostics = new List<DiagnosticInfo>();
        var hostEntitySymbol = compilation.GetTypeByMetadataName(hostModel.FullEntityTypeName);

        var mergedProperties = new List<PropertyMappingModel>(hostModel.Properties);

        // Seeded with the host so a [MapNested] whose target is the host itself trips the cycle
        // check on the first recursive entry rather than infinite-looping.
        var visitedFilterClasses = new HashSet<string>(StringComparer.Ordinal);
        var hostFqn = string.IsNullOrEmpty(hostModel.Namespace)
            ? hostModel.ClassName
            : hostModel.Namespace + "." + hostModel.ClassName;
        visitedFilterClasses.Add(hostFqn);

        foreach (var nested in hostModel.NestedMappings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var targetModel = ResolveTargetForNested(nested, hostEntitySymbol, allHostExtractionResults, newDiagnostics);
            if (targetModel is null) continue;

            var spliced = SpliceMappings(
                targetModel,
                nested,
                accumulatedClrPath: nested.NavigationPropertyName,
                accumulatedAliasPath: nested.Prefix,
                allHostExtractionResults,
                compilation,
                newDiagnostics,
                visitedFilterClasses,
                cancellationToken);
            mergedProperties.AddRange(spliced);
        }

        // FN0001 fires on either axis: same CLR accessor (would emit two predicates against the same
        // entity member) or same wire dispatch key (would shadow each other at apply time).
        var reportedGroupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var clrPathGroups = mergedProperties
            .GroupBy(mapping => mapping.PropertyName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);
        foreach (var duplicateGroup in clrPathGroups)
        {
            ReportDuplicate(duplicateGroup.Key, duplicateGroup.ToList());
        }
        var wireKeyGroups = mergedProperties
            .GroupBy(WireKeyOf, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);
        foreach (var duplicateGroup in wireKeyGroups)
        {
            ReportDuplicate(duplicateGroup.Key, duplicateGroup.ToList());
        }

        void ReportDuplicate(string groupKey, List<PropertyMappingModel> conflicts)
        {
            // A pair colliding on both CLR and wire axes would otherwise fire FN0001 twice.
            if (!reportedGroupKeys.Add(groupKey)) return;
            var sourcesFormatted = string.Join(", ", conflicts.Select(FormatSource));
            var locationsForReporting = new List<Location>();
            foreach (var mapping in conflicts)
            {
                var location = mapping.InliningSiteLocation?.ToLocation()
                    ?? mapping.DeclarationLocation?.ToLocation();
                if (location is not null)
                {
                    locationsForReporting.Add(location);
                }
            }

            var primaryLocation = locationsForReporting.Count > 0 ? locationsForReporting[0] : Location.None;
            var additionalLocations = locationsForReporting.Count > 1
                ? locationsForReporting.Skip(1).ToArray()
                : Array.Empty<Location>();

            newDiagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.DuplicateMapping,
                primaryLocation,
                additionalLocations,
                groupKey,
                hostFqn,
                sourcesFormatted));
        }

        // A spliced typed-value operator forces JsonSerializerOptions threading even when the host's
        // own extraction said no — the host extractor ran before splice and couldn't see it.
        var mergedHasAnyTypedValueProperty = hostModel.HasAnyTypedValueProperty
            || mergedProperties.Exists(mapping => mapping.HasTypedValueOperator);

        return new ResolvedHost(
            hostModel with
            {
                Properties = new EquatableList<PropertyMappingModel>(mergedProperties),
                HasAnyTypedValueProperty = mergedHasAnyTypedValueProperty,
            },
            new EquatableList<DiagnosticInfo>(newDiagnostics));
    }

    private static string WireKeyOf(PropertyMappingModel mapping)
        => string.IsNullOrEmpty(mapping.Alias) ? mapping.PropertyName : mapping.Alias!;

    private static string FormatSource(PropertyMappingModel mapping)
    {
        if (mapping.SourceFilterClassFqn is null)
        {
            return $"[Map] {mapping.ConfigurationMethodName}";
        }
        var sourceShortName = mapping.SourceFilterClassFqn.Substring(
            mapping.SourceFilterClassFqn.LastIndexOf('.') + 1);
        return $"[MapNested] {mapping.ConfigurationMethodName} (from {sourceShortName})";
    }

    private static FilterClassModel? ResolveTargetForNested(
        NestedMappingModel nested,
        INamedTypeSymbol? hostEntitySymbol,
        ImmutableArray<FilterClassModelWithDiagnostics> allHostExtractionResults,
        List<DiagnosticInfo> diagnosticsSink)
    {
        var navProperty = hostEntitySymbol?
            .GetMembers(nested.NavigationPropertyName)
            .OfType<IPropertySymbol>()
            .FirstOrDefault();

        if (navProperty is null || IsPrimitiveOrValueType(navProperty.Type))
        {
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedNavigationInvalid,
                nested.AttributeLocation?.ToLocation() ?? Location.None,
                nested.NavigationPropertyName,
                hostEntitySymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty) ?? string.Empty));
            return null;
        }

        // Collection navigations would need Any/All quantifier semantics — out of scope for v1.
        if (IsCollectionType(navProperty.Type))
        {
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedCollectionUnsupported,
                nested.AttributeLocation?.ToLocation() ?? Location.None,
                nested.NavigationPropertyName));
            return null;
        }

        var navTypeFqn = navProperty.Type
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty);
        var candidates = new List<FilterClassModel>();
        foreach (var candidateExtractionResult in allHostExtractionResults)
        {
            if (candidateExtractionResult.Model is null) continue;
            if (candidateExtractionResult.Model.FullEntityTypeName != navTypeFqn) continue;
            candidates.Add(candidateExtractionResult.Model);
        }

        if (nested.ExplicitFilterClassFqn is not null)
        {
            foreach (var candidate in candidates)
            {
                var candidateFqn = string.IsNullOrEmpty(candidate.Namespace)
                    ? candidate.ClassName
                    : candidate.Namespace + "." + candidate.ClassName;
                if (string.Equals(candidateFqn, nested.ExplicitFilterClassFqn, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedCrossAssembly,
                nested.AttributeLocation?.ToLocation() ?? Location.None,
                nested.ExplicitFilterClassFqn));
            return null;
        }

        if (candidates.Count == 0)
        {
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedTargetNotFound,
                nested.AttributeLocation?.ToLocation() ?? Location.None,
                nested.NavigationPropertyName,
                navTypeFqn));
            return null;
        }
        if (candidates.Count > 1)
        {
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedAmbiguous,
                nested.AttributeLocation?.ToLocation() ?? Location.None,
                nested.NavigationPropertyName,
                candidates.Count.ToString(CultureInfo.InvariantCulture),
                navTypeFqn));
            return null;
        }
        return candidates[0];
    }

    // accumulatedClrPath threads verbatim navigation names so emitted lambdas hit real entity members;
    // accumulatedAliasPath threads user-specified Prefix so wire dispatch keys honour it. The two
    // coincide when Prefix == NavigationPropertyName. Only/Except/DisableSorting apply to the
    // immediate target's direct mappings; transitive splices honour each inner [MapNested]'s own.
    private static List<PropertyMappingModel> SpliceMappings(
        FilterClassModel target,
        NestedMappingModel originalNested,
        string accumulatedClrPath,
        string accumulatedAliasPath,
        ImmutableArray<FilterClassModelWithDiagnostics> allHostExtractionResults,
        Compilation compilation,
        List<DiagnosticInfo> diagnosticsSink,
        HashSet<string> visitedFilterClasses,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var output = new List<PropertyMappingModel>();

        var targetClassFqn = string.IsNullOrEmpty(target.Namespace)
            ? target.ClassName
            : target.Namespace + "." + target.ClassName;

        if (!visitedFilterClasses.Add(targetClassFqn))
        {
            var cyclePath = string.Join(" -> ", visitedFilterClasses.Append(targetClassFqn));
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedCycle,
                originalNested.AttributeLocation?.ToLocation() ?? Location.None,
                cyclePath));
            return output;
        }

        try
        {
            foreach (var mapping in target.Properties)
            {
                if (!IsPathAllowed(mapping.PropertyName, originalNested.Only, originalNested.Except)) continue;

                var splicedPropertyName = accumulatedClrPath + "." + mapping.PropertyName;
                var childWireKey = string.IsNullOrEmpty(mapping.Alias) ? mapping.PropertyName : mapping.Alias!;
                var splicedAlias = accumulatedAliasPath + "." + childWireKey;
                var sortable = !originalNested.DisableSorting && mapping.Sortable;
                output.Add(mapping with
                {
                    PropertyName = splicedPropertyName,
                    Alias = splicedAlias,
                    Sortable = sortable,
                    InliningSiteLocation = originalNested.AttributeLocation,
                    SourceFilterClassFqn = targetClassFqn,
                    ConfigurationMethodName = originalNested.HostMethodName,
                });
            }

            var targetEntitySymbol = compilation.GetTypeByMetadataName(target.FullEntityTypeName);
            foreach (var transitive in target.NestedMappings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var transitiveTarget = ResolveTargetForNested(transitive, targetEntitySymbol, allHostExtractionResults, diagnosticsSink);
                if (transitiveTarget is null) continue;

                output.AddRange(SpliceMappings(
                    transitiveTarget,
                    transitive,
                    accumulatedClrPath: accumulatedClrPath + "." + transitive.NavigationPropertyName,
                    accumulatedAliasPath: accumulatedAliasPath + "." + transitive.Prefix,
                    allHostExtractionResults,
                    compilation,
                    diagnosticsSink,
                    visitedFilterClasses,
                    cancellationToken));
            }
        }
        finally
        {
            // Pop on exit so sibling branches can legitimately re-enter the same target.
            visitedFilterClasses.Remove(targetClassFqn);
        }

        return output;
    }

    private static bool IsPathAllowed(string relativePath, EquatableList<string> only, EquatableList<string> except)
    {
        if (only.Count > 0)
        {
            var matched = false;
            foreach (var allowed in only)
            {
                if (string.Equals(allowed, relativePath, StringComparison.OrdinalIgnoreCase))
                {
                    matched = true;
                    break;
                }
            }
            if (!matched) return false;
        }
        foreach (var blocked in except)
        {
            if (string.Equals(blocked, relativePath, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    // Leaf types (string, primitives, structs like DateTime/Guid) belong to [Map]/[PropertyMap], not [MapNested].
    private static bool IsPrimitiveOrValueType(ITypeSymbol type)
    {
        if (type.SpecialType != SpecialType.None) return true;
        if (type.IsValueType) return true;
        if (type.ContainingNamespace?.ToDisplayString() == "System") return true;
        return false;
    }

    // System.String implements IEnumerable<char> but is a leaf, not a collection nav.
    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol) return true;
        if (type is INamedTypeSymbol named)
        {
            if (named.SpecialType == SpecialType.System_String) return false;
            foreach (var implementedInterface in named.AllInterfaces)
            {
                if (implementedInterface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
