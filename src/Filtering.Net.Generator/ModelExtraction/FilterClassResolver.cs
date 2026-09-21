namespace Filtering.Net.Generator;

// Phase two of filter-class extraction: resolves every [Map] against the compilation-wide index and
// applies the assembly-level defaults. Runs in a node combined with GeneratorIndex, so a profile or
// an [assembly: FilterDefaults] added in any file re-runs it for every class.
internal static class FilterClassResolver
{
    private const int FallbackDefaultPageSize = 50;
    private const int FallbackMaxPageSize = 200;
    private const int FallbackMaxNestingDepth = 10;
    private const int FallbackMaxLeafConditions = 50;

    public static FilterClassModelWithDiagnostics Resolve(
        FilterClassDeclarationWithDiagnostics extractionResult,
        GeneratorIndex index,
        CancellationToken cancellationToken)
    {
        if (extractionResult.Declaration is null)
        {
            return new FilterClassModelWithDiagnostics(Model: null, Diagnostics: extractionResult.Diagnostics);
        }

        var declaration = extractionResult.Declaration;
        var diagnostics = new List<DiagnosticInfo>(extractionResult.Diagnostics);
        var properties = new List<PropertyMappingModel>(declaration.Properties.Count);

        foreach (var propertyDeclaration in declaration.Properties)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mappingResult = PropertyMappingExtractor.ResolveMapping(propertyDeclaration, index);
            diagnostics.AddRange(mappingResult.Diagnostics);
            if (mappingResult.Model is not null) properties.Add(mappingResult.Model);
        }

        DetectAliasCollisions(properties, declaration, diagnostics);
        DetectMissingSortable(properties, declaration, diagnostics);

        // Class-level flag: any custom operator with a non-null value type needs typed JSON
        // deserialisation; the emitter uses it to decide whether to thread JsonSerializerOptions.
        var hasAnyTypedValueProperty =
            properties.Exists(propertyMappingModel => propertyMappingModel.HasTypedValueOperator)
            || declaration.Overrides.Any(propertyOverrideModel => propertyOverrideModel.HasTypedValueOperator);

        var assemblyDefaults = index.AssemblyDefaults;

        var model = new FilterClassModel(
            Namespace: declaration.Namespace,
            ClassName: declaration.ClassName,
            FullEntityTypeName: declaration.FullEntityTypeName,
            MaxPageSize: declaration.ClassMaxPageSize ?? assemblyDefaults.MaxPageSize ?? FallbackMaxPageSize,
            DefaultPageSize: declaration.ClassDefaultPageSize ?? assemblyDefaults.DefaultPageSize ?? FallbackDefaultPageSize,
            MaxNestingDepth: assemblyDefaults.MaxNestingDepth ?? FallbackMaxNestingDepth,
            MaxLeafConditions: assemblyDefaults.MaxLeafConditions ?? FallbackMaxLeafConditions,
            Properties: new EquatableList<PropertyMappingModel>(properties),
            Interceptors: declaration.Interceptors,
            Overrides: declaration.Overrides,
            Location: declaration.Location,
            HasAnyTypedValueProperty: hasAnyTypedValueProperty,
            NestedMappings: declaration.NestedMappings);

        return new FilterClassModelWithDiagnostics(
            Model: model,
            Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
    }

    private static void DetectAliasCollisions(
        List<PropertyMappingModel> properties,
        FilterClassDeclaration declaration,
        List<DiagnosticInfo> diagnostics)
    {
        // Tracks every site that has claimed a given case-folded name (property name or alias);
        // a fresh collision reports every prior claimant as an additional location.
        var nameToSites = new Dictionary<string, List<LocationInfo>>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in properties)
        {
            if (!nameToSites.TryGetValue(mapping.PropertyName, out var bucket))
            {
                bucket = [];
                nameToSites[mapping.PropertyName] = bucket;
            }
            if (mapping.DeclarationLocation is not null) bucket.Add(mapping.DeclarationLocation);
        }
        foreach (var mapping in properties)
        {
            if (string.IsNullOrEmpty(mapping.Alias)) continue;
            var aliasLocation = mapping.DeclarationLocation;
            if (nameToSites.TryGetValue(mapping.Alias!, out var existingSites))
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.AliasCollision,
                    aliasLocation ?? declaration.Location,
                    existingSites.ToArray(),
                    mapping.Alias!,
                    declaration.FullEntityTypeName));
                if (aliasLocation is not null) existingSites.Add(aliasLocation);
            }
            else
            {
                var bucket = new List<LocationInfo>();
                if (aliasLocation is not null) bucket.Add(aliasLocation);
                nameToSites[mapping.Alias!] = bucket;
            }
        }
    }

    private static void DetectMissingSortable(
        List<PropertyMappingModel> properties,
        FilterClassDeclaration declaration,
        List<DiagnosticInfo> diagnostics)
    {
        foreach (var mapping in properties)
        {
            if (mapping.Sortable) continue;
            if (!IsLikelySortableType(mapping.PropertyClrType)) continue;
            // Anchored at the [Map] so the warning is navigable and #pragma-suppressible per property.
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NotSortableLikelyOmission,
                mapping.DeclarationLocation ?? declaration.Location,
                mapping.PropertyName,
                mapping.PropertyClrType));
        }
    }

    private static bool IsLikelySortableType(string propertyClrType)
    {
        // Strip a single trailing '?' (nullable annotation in the display string).
        var bareType = propertyClrType.EndsWith("?", StringComparison.Ordinal)
            ? propertyClrType[..^1]
            : propertyClrType;
        return bareType is "System.DateTime"
            or "System.DateTimeOffset"
            or "System.DateOnly"
            or "int"
            or "long"
            or "short"
            or "decimal"
            or "double"
            or "float";
    }
}
