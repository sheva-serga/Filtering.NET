using System.Collections.Immutable;
using System.Globalization;

namespace Filtering.Net.Generator;

// Splices every [MapNested] target's mappings into the host so duplicates and cycles can be found at
// build time. Works on models only: the navigation was already classified against the entity symbol
// during extraction, so this node needs no Compilation and re-runs only when a model actually changes.
internal static class NestedFilterResolver
{
    // MaxDepth expands one nesting that many times along a single path, and the DFS below recurses
    // once per level. Anything past this is an authoring mistake, not a filter graph.
    public const int MaxSupportedNestingDepth = 64;

    public sealed record ResolvedHost(
        FilterClassModel Model,
        EquatableList<DiagnosticInfo> Diagnostics);

    public static ResolvedHost Resolve(
        FilterClassModelWithDiagnostics hostExtractionResult,
        ImmutableArray<FilterClassModelWithDiagnostics> allHostExtractionResults,
        CancellationToken cancellationToken)
    {
        if (hostExtractionResult.Model is null)
        {
            return new ResolvedHost(hostExtractionResult.Model!, new EquatableList<DiagnosticInfo>());
        }

        var hostModel = hostExtractionResult.Model;
        var newDiagnostics = new List<DiagnosticInfo>();

        var mergedProperties = new List<PropertyMappingModel>(hostModel.Properties);
        var resolvedNestedMappings = new List<NestedMappingModel>(hostModel.NestedMappings.Count);
        var typedValueTracker = new TypedValueTracker();

        var hostFqn = ClassFqnOf(hostModel);
        var mappingSources = SeedHostMappingSources(hostModel);

        // Seeded with the host so a [MapNested] whose target is the host itself trips the cycle
        // check on the first recursive entry rather than infinite-looping. The parallel location
        // stack mirrors the visited-set so cycle diagnostics can report every [MapNested] site
        // along the path as additionalLocations.
        var nestingPath = new List<NestingPathStep>();
        var nestedSiteStack = new Stack<LocationInfo?>();
        nestingPath.Add(new NestingPathStep(hostFqn, EnteredThroughNestingKey: null, EnteredThroughBoundedNesting: false));
        nestedSiteStack.Push(null);

        foreach (var nested in hostModel.NestedMappings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var keyedNested = nested with { NestingKey = NestingKeyOf(hostFqn, nested) };

            // FN0021: an out-of-range MaxDepth is rejected before the DFS runs, because a huge bound
            // on a self-referencing navigation would recurse until the analyzer process dies.
            if (nested.MaxDepth < 0 || nested.MaxDepth > MaxSupportedNestingDepth)
            {
                newDiagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.NestedMaxDepthInvalid,
                    nested.AttributeLocation,
                    nested.NavigationPropertyName,
                    nested.MaxDepth.ToString(CultureInfo.InvariantCulture),
                    MaxSupportedNestingDepth.ToString(CultureInfo.InvariantCulture)));
                resolvedNestedMappings.Add(keyedNested);
                continue;
            }

            var targetModel = ResolveTargetForNested(nested, hostModel.FullEntityTypeName, allHostExtractionResults, newDiagnostics);
            if (targetModel is null)
            {
                resolvedNestedMappings.Add(keyedNested);
                continue;
            }
            resolvedNestedMappings.Add(keyedNested with { ResolvedTargetClassFqn = ClassFqnOf(targetModel) });

            ReportUnknownPathFilters(nested, targetModel, newDiagnostics);

            var spliced = SpliceMappings(
                targetModel,
                keyedNested,
                accumulatedClrPath: nested.NavigationPropertyName,
                accumulatedAliasPath: nested.Prefix,
                allHostExtractionResults,
                newDiagnostics,
                nestingPath,
                nestedSiteStack,
                typedValueTracker,
                mappingSources,
                cancellationToken);
            mergedProperties.AddRange(spliced);
        }

        ReportDuplicateMappings(mappingSources, hostFqn, newDiagnostics);

        // A spliced typed-value operator forces JsonSerializerOptions threading even when the host's
        // own extraction said no — the host extractor ran before splice and couldn't see it.
        var mergedHasAnyTypedValueProperty = hostModel.HasAnyTypedValueProperty
            || typedValueTracker.NestedFilterNeedsSerializerOptions
            || mergedProperties.Exists(mapping => mapping.HasTypedValueOperator);

        return new ResolvedHost(
            hostModel with
            {
                Properties = new EquatableList<PropertyMappingModel>(mergedProperties),
                NestedMappings = new EquatableList<NestedMappingModel>(resolvedNestedMappings),
                HasAnyTypedValueProperty = mergedHasAnyTypedValueProperty,
            },
            new EquatableList<DiagnosticInfo>(newDiagnostics));
    }

    // What claimed a filter path on this host. [PropertyMap] rules take part because FilterSchema
    // registers them exactly like a [Map], even though they never enter the merged property list.
    private enum MappingSourceKind
    {
        HostMap,
        HostRule,
        Spliced,
    }

    private sealed record MappingSource(
        MappingSourceKind Kind,
        string ClrPath,
        string? Alias,
        string Label,
        LocationInfo? Location);

    private static List<MappingSource> SeedHostMappingSources(FilterClassModel hostModel)
    {
        var mappingSources = new List<MappingSource>(hostModel.Properties.Count + hostModel.Overrides.Count);
        var mappedPropertyNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var mapping in hostModel.Properties)
        {
            mappedPropertyNames.Add(mapping.PropertyName);
            mappingSources.Add(new MappingSource(
                MappingSourceKind.HostMap,
                mapping.PropertyName,
                mapping.Alias,
                $"[Map] {mapping.DeclarationName}",
                mapping.DeclarationLocation));
        }

        foreach (var propertyOverride in hostModel.Overrides)
        {
            // Mirrors emission: an override with an unusable signature (FN0023) or one shadowed by a
            // [Map] (FN0002) never reaches the schema, so it cannot collide with anything.
            if (propertyOverride.BuilderTypeFqn is null) continue;
            if (mappedPropertyNames.Contains(propertyOverride.PropertyName)) continue;
            mappingSources.Add(new MappingSource(
                MappingSourceKind.HostRule,
                propertyOverride.PropertyName,
                Alias: null,
                $"[PropertyMap] {propertyOverride.PropertyName}",
                propertyOverride.DeclarationLocation));
        }

        return mappingSources;
    }

    // FN0001 fires on either axis: same CLR accessor (would emit two predicates against the same
    // entity member) or same wire dispatch key (would shadow each other at apply time). The wire axis
    // is the exact set FilterSchema registers — a property's own path and its alias, both.
    private static void ReportDuplicateMappings(
        List<MappingSource> mappingSources,
        string hostFqn,
        List<DiagnosticInfo> diagnostics)
    {
        var reportedGroupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var duplicateGroup in mappingSources
            .GroupBy(source => source.ClrPath, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1))
        {
            ReportDuplicate(duplicateGroup.Key, duplicateGroup.ToList());
        }

        var wireKeyClaimants = new Dictionary<string, List<MappingSource>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in mappingSources)
        {
            ClaimWireKey(source.ClrPath, source);
            if (!string.IsNullOrEmpty(source.Alias)) ClaimWireKey(source.Alias!, source);
        }
        foreach (var claim in wireKeyClaimants)
        {
            if (claim.Value.Count <= 1) continue;
            // A collision purely between two [Map]s on this class is FN0009's alias rule, which names
            // the offending alias; reporting FN0001 as well would be two ids for one mistake.
            if (claim.Value.TrueForAll(source => source.Kind == MappingSourceKind.HostMap)) continue;
            ReportDuplicate(claim.Key, claim.Value);
        }

        void ClaimWireKey(string wireKey, MappingSource source)
        {
            if (!wireKeyClaimants.TryGetValue(wireKey, out var claimants))
            {
                claimants = [];
                wireKeyClaimants[wireKey] = claimants;
            }
            if (!claimants.Contains(source)) claimants.Add(source);
        }

        void ReportDuplicate(string groupKey, List<MappingSource> conflicts)
        {
            // A pair colliding on both CLR and wire axes would otherwise fire FN0001 twice.
            if (!reportedGroupKeys.Add(groupKey)) return;
            var sourcesFormatted = string.Join(", ", conflicts.Select(source => source.Label));
            var locationsForReporting = new List<LocationInfo>();
            foreach (var source in conflicts)
            {
                if (source.Location is not null) locationsForReporting.Add(source.Location);
            }

            var primaryLocation = locationsForReporting.Count > 0 ? locationsForReporting[0] : null;
            var additionalLocations = locationsForReporting.Count > 1
                ? locationsForReporting.Skip(1).ToArray()
                : Array.Empty<LocationInfo>();

            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.DuplicateMapping,
                primaryLocation,
                additionalLocations,
                groupKey,
                hostFqn,
                sourcesFormatted));
        }
    }

    // FN0026: mirrors FilterSchema.LiftInto, which throws for a dotless Only/Except entry the nested
    // filter does not expose. A dotted entry names a path a further [MapNested] contributes, and a
    // bounded nesting legitimately drops those, so only dotless entries can be checked.
    private static void ReportUnknownPathFilters(
        NestedMappingModel nested,
        FilterClassModel target,
        List<DiagnosticInfo> diagnostics)
    {
        var mappedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in target.Properties)
        {
            if (mapping.SourceFilterClassFqn is null) mappedPaths.Add(mapping.PropertyName);
        }
        foreach (var propertyOverride in target.Overrides)
        {
            if (propertyOverride.BuilderTypeFqn is not null) mappedPaths.Add(propertyOverride.PropertyName);
        }

        ReportUnknownEntries(nested.Only, "Only");
        ReportUnknownEntries(nested.Except, "Except");

        void ReportUnknownEntries(EquatableList<string> entries, string optionName)
        {
            foreach (var entry in entries)
            {
                if (entry.IndexOf('.') >= 0 || mappedPaths.Contains(entry)) continue;
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.NestedPathFilterUnknown,
                    nested.AttributeLocation,
                    nested.NavigationPropertyName,
                    optionName,
                    entry,
                    ClassFqnOf(target)));
            }
        }
    }

    // A nested filter's [PropertyMap] rules are lifted at runtime but never spliced into the merged
    // property list, so their typed values have to be tracked on the side.
    private sealed class TypedValueTracker
    {
        public bool NestedFilterNeedsSerializerOptions { get; set; }
    }

    // One class on the current expansion path, with the nesting it was reached through.
    private sealed record NestingPathStep(string ClassFqn, string? EnteredThroughNestingKey, bool EnteredThroughBoundedNesting);

    // Identifies one [MapNested] declaration; the same navigation may be nested twice under different prefixes.
    private static string NestingKeyOf(string declaringClassFqn, NestedMappingModel nested) =>
        nested.Prefix == nested.NavigationPropertyName
            ? declaringClassFqn + "." + nested.NavigationPropertyName
            : declaringClassFqn + "." + nested.NavigationPropertyName + "@" + nested.Prefix;

    private static string ClassFqnOf(FilterClassModel model) =>
        string.IsNullOrEmpty(model.Namespace) ? model.ClassName : model.Namespace + "." + model.ClassName;

    private static FilterClassModel? ResolveTargetForNested(
        NestedMappingModel nested,
        string declaringEntityFullName,
        ImmutableArray<FilterClassModelWithDiagnostics> allHostExtractionResults,
        List<DiagnosticInfo> diagnosticsSink)
    {
        if (nested.NavigationKind is NestedNavigationKind.Missing or NestedNavigationKind.PrimitiveOrValue)
        {
            // A missing navigation has no declaration to point at; a found-but-primitive one does.
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedNavigationInvalid,
                nested.AttributeLocation,
                new[] { nested.NavigationLocation },
                nested.NavigationPropertyName,
                declaringEntityFullName));
            return null;
        }

        // Collection navigations would need Any/All quantifier semantics — out of scope for v1.
        if (nested.NavigationKind == NestedNavigationKind.Collection)
        {
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedCollectionUnsupported,
                nested.AttributeLocation,
                new[] { nested.NavigationLocation },
                nested.NavigationPropertyName));
            return null;
        }

        var navTypeFqn = nested.NavigationTypeFullName ?? string.Empty;
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
                if (string.Equals(ClassFqnOf(candidate), nested.ExplicitFilterClassFqn, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }
            // Surface the explicit T type's declaration when it lives in this compilation; a type
            // from a referenced assembly has no syntax-tree location and contributes nothing.
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedFilterClassUnusable,
                nested.AttributeLocation,
                new[] { nested.ExplicitFilterClassLocation },
                nested.ExplicitFilterClassFqn,
                nested.NavigationPropertyName,
                navTypeFqn));
            return null;
        }

        if (candidates.Count == 0)
        {
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedTargetNotFound,
                nested.AttributeLocation,
                new[] { nested.NavigationLocation },
                nested.NavigationPropertyName,
                navTypeFqn));
            return null;
        }
        if (candidates.Count > 1)
        {
            var candidateLocations = new List<LocationInfo?>(candidates.Count);
            foreach (var candidate in candidates)
            {
                candidateLocations.Add(candidate.Location);
            }
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedAmbiguous,
                nested.AttributeLocation,
                candidateLocations,
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
        List<DiagnosticInfo> diagnosticsSink,
        List<NestingPathStep> nestingPath,
        Stack<LocationInfo?> nestedSiteStack,
        TypedValueTracker typedValueTracker,
        List<MappingSource> mappingSources,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var output = new List<PropertyMappingModel>();
        typedValueTracker.NestedFilterNeedsSerializerOptions |= target.HasAnyTypedValueProperty;

        var targetClassFqn = ClassFqnOf(target);
        var nestingKey = originalNested.NestingKey!;
        var isBoundedNesting = originalNested.MaxDepth > 0;

        // Mirrors FilterNestingContext.TryEnter at runtime: a bounded nesting that used up its depth on
        // this path simply contributes nothing more.
        if (isBoundedNesting
            && nestingPath.Count(step => step.EnteredThroughNestingKey == nestingKey) >= originalNested.MaxDepth)
        {
            return output;
        }

        // Re-entering a class is a real cycle only when no nesting since its last visit is bounded.
        var previousVisitIndex = nestingPath.FindLastIndex(step => step.ClassFqn == targetClassFqn);
        var closesUnboundedCycle = previousVisitIndex >= 0
            && !isBoundedNesting
            && !nestingPath.Skip(previousVisitIndex + 1).Any(step => step.EnteredThroughBoundedNesting);
        if (closesUnboundedCycle)
        {
            var cyclePath = string.Join(" -> ", nestingPath.Select(step => step.ClassFqn).Append(targetClassFqn));
            // The currently-being-visited [MapNested] is the primary squiggle; every previously-
            // pushed site on the DFS path becomes additional, in declaration order.
            var pathLocations = new List<LocationInfo?>();
            foreach (var stackedLocation in nestedSiteStack.Reverse())
            {
                if (stackedLocation is not null) pathLocations.Add(stackedLocation);
            }
            diagnosticsSink.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NestedCycle,
                originalNested.AttributeLocation,
                pathLocations,
                cyclePath));
            return output;
        }
        nestingPath.Add(new NestingPathStep(targetClassFqn, nestingKey, isBoundedNesting));
        nestedSiteStack.Push(originalNested.AttributeLocation);

        try
        {
            foreach (var mapping in target.Properties)
            {
                if (!IsPathAllowed(mapping.PropertyName, originalNested.HasOnly, originalNested.Only, originalNested.Except)) continue;

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
                    DeclarationName = originalNested.NavigationPropertyName,
                });
                mappingSources.Add(new MappingSource(
                    MappingSourceKind.Spliced,
                    splicedPropertyName,
                    splicedAlias,
                    $"[MapNested] {originalNested.NavigationPropertyName} (from {ShortNameOf(targetClassFqn)})",
                    originalNested.AttributeLocation));
            }

            // The target's [PropertyMap] rules are lifted at runtime under the same prefix, so they
            // claim filter paths on the host even though they never join the merged property list.
            foreach (var propertyOverride in target.Overrides)
            {
                if (propertyOverride.BuilderTypeFqn is null) continue;
                if (!IsPathAllowed(propertyOverride.PropertyName, originalNested.HasOnly, originalNested.Only, originalNested.Except)) continue;
                mappingSources.Add(new MappingSource(
                    MappingSourceKind.Spliced,
                    accumulatedClrPath + "." + propertyOverride.PropertyName,
                    accumulatedAliasPath + "." + propertyOverride.PropertyName,
                    $"[PropertyMap] {propertyOverride.PropertyName} (from {ShortNameOf(targetClassFqn)})",
                    originalNested.AttributeLocation));
            }

            foreach (var transitive in target.NestedMappings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (transitive.MaxDepth < 0 || transitive.MaxDepth > MaxSupportedNestingDepth) continue;

                // A broken [MapNested] on the target is reported by the target's own resolution pass;
                // re-reporting it here would repeat it once per host that reaches this class.
                var transitiveDiagnostics = new List<DiagnosticInfo>();
                var transitiveTarget = ResolveTargetForNested(
                    transitive,
                    target.FullEntityTypeName,
                    allHostExtractionResults,
                    transitiveDiagnostics);
                if (transitiveTarget is null) continue;

                output.AddRange(SpliceMappings(
                    transitiveTarget,
                    transitive with { NestingKey = NestingKeyOf(targetClassFqn, transitive) },
                    accumulatedClrPath: accumulatedClrPath + "." + transitive.NavigationPropertyName,
                    accumulatedAliasPath: accumulatedAliasPath + "." + transitive.Prefix,
                    allHostExtractionResults,
                    diagnosticsSink,
                    nestingPath,
                    nestedSiteStack,
                    typedValueTracker,
                    mappingSources,
                    cancellationToken));
            }
        }
        finally
        {
            // Pop on exit so sibling branches can legitimately re-enter the same target.
            nestingPath.RemoveAt(nestingPath.Count - 1);
            nestedSiteStack.Pop();
        }

        return output;
    }

    private static string ShortNameOf(string classFqn) =>
        classFqn.Substring(classFqn.LastIndexOf('.') + 1);

    private static bool IsPathAllowed(string relativePath, bool hasOnly, EquatableList<string> only, EquatableList<string> except)
    {
        if (hasOnly)
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
}
