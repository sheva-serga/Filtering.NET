using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

internal static class FilterClassExtractor
{
    private const string MapAttributeFullName = "Filtering.Net.MapAttribute";
    private const string InterceptValueAttributeFullName = "Filtering.Net.InterceptValueAttribute";
    private const string PropertyMapAttributeFullName = "Filtering.Net.PropertyMapAttribute";
    private const string PageSettingsAttributeFullName = "Filtering.Net.PageSettingsAttribute";
    private const string FilterDefaultsAttributeFullName = "Filtering.Net.FilterDefaultsAttribute";

    private const int FallbackDefaultPageSize = 50;
    private const int FallbackMaxPageSize = 200;

    public static FilterClassModelWithDiagnostics Extract(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var diagnostics = new List<DiagnosticInfo>();

        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return new FilterClassModelWithDiagnostics(Model: null, Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
        }

        var attributeData = context.Attributes.FirstOrDefault();
        if (attributeData?.AttributeClass is null || attributeData.AttributeClass.TypeArguments.Length != 1)
        {
            return new FilterClassModelWithDiagnostics(Model: null, Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
        }

        if (attributeData.AttributeClass.TypeArguments[0] is not INamedTypeSymbol entityType)
        {
            return new FilterClassModelWithDiagnostics(Model: null, Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // -------- Page settings (class override + assembly default) --------
        var (defaultPageSize, maxPageSize) = ResolvePageSettings(classSymbol, context.SemanticModel.Compilation.Assembly);

        var compilation = context.SemanticModel.Compilation;

        // Feed virtual enum profiles so the index can detect collisions between hand-written
        // [FilterProfile<MyEnum>] and auto-emitted Filtering.Net.Generated.<EnumName>Filter (FN0014).
        var virtualEnumProfiles = EnumTypeCollector.Collect(compilation);
        var profileIndex = ProfileIndexBuilder.Build(compilation, virtualEnumProfiles);

        // -------- Walk methods --------
        var properties = new List<PropertyMappingModel>();
        var interceptors = new List<InterceptorModel>();
        var overrides = new List<PropertyOverrideModel>();

        var mappedPropertyFirstMethodName = new Dictionary<string, string>(StringComparer.Ordinal);
        var mappedPropertyFirstLocation = new Dictionary<string, Location?>(StringComparer.Ordinal);
        var interceptedPropertyFirstLocation = new Dictionary<string, Location?>(StringComparer.Ordinal);
        var propertyMapFirstLocation = new Dictionary<string, Location?>(StringComparer.Ordinal);

        foreach (var member in classSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is not IMethodSymbol methodSymbol) continue;

            var memberAttributes = methodSymbol.GetAttributes();
            var mapAttribute = FindAttribute(memberAttributes, MapAttributeFullName);
            var interceptAttribute = FindAttribute(memberAttributes, InterceptValueAttributeFullName);
            var propertyMapAttribute = FindAttribute(memberAttributes, PropertyMapAttributeFullName);

            if (mapAttribute is not null)
            {
                ExtractMapMethod(
                    methodSymbol,
                    classSymbol,
                    entityType,
                    mapAttribute,
                    compilation,
                    profileIndex,
                    diagnostics,
                    properties,
                    mappedPropertyFirstMethodName,
                    mappedPropertyFirstLocation);
            }

            if (interceptAttribute is not null)
            {
                ExtractInterceptorMethod(
                    methodSymbol,
                    interceptAttribute,
                    diagnostics,
                    interceptors,
                    interceptedPropertyFirstLocation);
            }

            if (propertyMapAttribute is not null)
            {
                ExtractPropertyOverrideMethod(
                    methodSymbol,
                    propertyMapAttribute,
                    compilation,
                    overrides,
                    propertyMapFirstLocation);
            }
        }

        // -------- Cross-method validations --------

        // FN0002: same property name appearing in both [Map] and [PropertyMap].
        // Primary squiggle on the [PropertyMap] site (more naturally available — it's the override
        // shadowing the regular [Map]); additional points at the colliding [Map] method.
        foreach (var sharedPropertyName in mappedPropertyFirstMethodName.Keys.Intersect(propertyMapFirstLocation.Keys, StringComparer.Ordinal))
        {
            propertyMapFirstLocation.TryGetValue(sharedPropertyName, out var propertyMapLocation);
            mappedPropertyFirstLocation.TryGetValue(sharedPropertyName, out var mapLocation);
            var additionalLocations = mapLocation is not null
                ? new[] { mapLocation }
                : Array.Empty<Location>();
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.MapAndPropertyMapBoth,
                propertyMapLocation ?? classSymbol.Locations.FirstOrDefault(),
                additionalLocations,
                sharedPropertyName));
        }

        DetectAliasCollisions(properties, classSymbol, entityType, diagnostics);

        DetectMissingSortable(properties, classSymbol, diagnostics);

        foreach (var interceptedEntry in interceptedPropertyFirstLocation)
        {
            if (!mappedPropertyFirstMethodName.ContainsKey(interceptedEntry.Key))
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.InterceptorWithoutMap,
                    interceptedEntry.Value ?? classSymbol.Locations.FirstOrDefault(),
                    interceptedEntry.Key));
            }
        }

        var classNamespace = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        // Class-level flag: any custom operator with a non-null value type needs typed JSON
        // deserialisation; the emitter uses it to decide whether to thread JsonSerializerOptions.
        var hasAnyTypedValueProperty =
            properties.Exists(propertyMappingModel => propertyMappingModel.HasTypedValueOperator)
            || overrides.Exists(propertyOverrideModel => propertyOverrideModel.HasTypedValueOperator);

        var nestedMappings = MapNestedExtractor.Extract(classSymbol, cancellationToken);

        var model = new FilterClassModel(
            Namespace: classNamespace,
            ClassName: classSymbol.Name,
            FullEntityTypeName: entityType.ToDisplayString(),
            MaxPageSize: maxPageSize,
            DefaultPageSize: defaultPageSize,
            Properties: new EquatableList<PropertyMappingModel>(properties),
            Interceptors: new EquatableList<InterceptorModel>(interceptors),
            Overrides: new EquatableList<PropertyOverrideModel>(overrides),
            Location: LocationInfo.FromLocation(classSymbol.Locations.FirstOrDefault()),
            HasAnyTypedValueProperty: hasAnyTypedValueProperty,
            NestedMappings: nestedMappings);

        return new FilterClassModelWithDiagnostics(
            Model: model,
            Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
    }

    private static void ExtractMapMethod(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol entityType,
        AttributeData mapAttribute,
        Compilation compilation,
        ProfileIndex profileIndex,
        List<DiagnosticInfo> diagnostics,
        List<PropertyMappingModel> properties,
        Dictionary<string, string> mappedPropertyFirstMethodName,
        Dictionary<string, Location?> mappedPropertyFirstLocation)
    {
        if (!IsPartial(methodSymbol))
        {
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.MissingPartial,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name));
        }

        var extractionResult = PropertyMappingExtractor.Extract(methodSymbol, entityType, mapAttribute, compilation, profileIndex);
        diagnostics.AddRange(extractionResult.Diagnostics);

        if (extractionResult.Model is null)
        {
            // Record the attempted name so a second [Map] for the same name fires FN0001.
            var attemptedName = ReadConstructorString(mapAttribute, position: 0);
            if (!string.IsNullOrEmpty(attemptedName))
            {
                EmitDuplicateMapDiagnosticIfNeeded(
                    methodSymbol,
                    classSymbol,
                    attemptedName!,
                    mappedPropertyFirstMethodName,
                    mappedPropertyFirstLocation,
                    diagnostics);
            }
            return;
        }

        if (mappedPropertyFirstMethodName.ContainsKey(extractionResult.Model.PropertyName))
        {
            EmitDuplicateMapDiagnosticIfNeeded(
                methodSymbol,
                classSymbol,
                extractionResult.Model.PropertyName,
                mappedPropertyFirstMethodName,
                mappedPropertyFirstLocation,
                diagnostics);
            return;
        }

        mappedPropertyFirstMethodName[extractionResult.Model.PropertyName] = methodSymbol.Name;
        mappedPropertyFirstLocation[extractionResult.Model.PropertyName] = methodSymbol.Locations.FirstOrDefault();
        properties.Add(extractionResult.Model);
    }

    private static void EmitDuplicateMapDiagnosticIfNeeded(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol classSymbol,
        string propertyName,
        Dictionary<string, string> mappedPropertyFirstMethodName,
        Dictionary<string, Location?> mappedPropertyFirstLocation,
        List<DiagnosticInfo> diagnostics)
    {
        var location = methodSymbol.Locations.FirstOrDefault();
        if (mappedPropertyFirstMethodName.TryGetValue(propertyName, out var previousMethodName))
        {
            mappedPropertyFirstLocation.TryGetValue(propertyName, out var previousLocation);
            var hostFqn = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
            var sources = "[Map] " + previousMethodName + ", [Map] " + methodSymbol.Name;
            var additionalLocations = previousLocation is not null
                ? new[] { previousLocation }
                : Array.Empty<Location>();
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.DuplicateMapping,
                location,
                additionalLocations,
                propertyName,
                hostFqn,
                sources));
            return;
        }
        mappedPropertyFirstMethodName[propertyName] = methodSymbol.Name;
        mappedPropertyFirstLocation[propertyName] = location;
    }

    private static void DetectAliasCollisions(
        List<PropertyMappingModel> properties,
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol entityType,
        List<DiagnosticInfo> diagnostics)
    {
        // Tracks every site that has claimed a given case-folded name (property name or alias);
        // a fresh collision reports every prior claimant as an additional location.
        var nameToSites = new Dictionary<string, List<Location>>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in properties)
        {
            var propertyLocation = mapping.DeclarationLocation?.ToLocation();
            if (!nameToSites.TryGetValue(mapping.PropertyName, out var bucket))
            {
                bucket = new List<Location>();
                nameToSites[mapping.PropertyName] = bucket;
            }
            if (propertyLocation is not null) bucket.Add(propertyLocation);
        }
        foreach (var mapping in properties)
        {
            if (string.IsNullOrEmpty(mapping.Alias)) continue;
            var aliasLocation = mapping.DeclarationLocation?.ToLocation();
            if (nameToSites.TryGetValue(mapping.Alias!, out var existingSites))
            {
                var primaryLocation = aliasLocation ?? classSymbol.Locations.FirstOrDefault();
                var additionalLocations = existingSites.ToArray();
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.AliasCollision,
                    primaryLocation,
                    additionalLocations,
                    mapping.Alias!,
                    entityType.ToDisplayString()));
                if (aliasLocation is not null) existingSites.Add(aliasLocation);
            }
            else
            {
                var bucket = new List<Location>();
                if (aliasLocation is not null) bucket.Add(aliasLocation);
                nameToSites[mapping.Alias!] = bucket;
            }
        }
    }

    private static void DetectMissingSortable(
        List<PropertyMappingModel> properties,
        INamedTypeSymbol classSymbol,
        List<DiagnosticInfo> diagnostics)
    {
        foreach (var mapping in properties)
        {
            if (mapping.Sortable) continue;
            if (!IsLikelySortableType(mapping.PropertyClrType)) continue;
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.NotSortableLikelyOmission,
                classSymbol.Locations.FirstOrDefault(),
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

    private static bool ReadSortableNamedArg(AttributeData mapAttribute)
    {
        foreach (var namedArgument in mapAttribute.NamedArguments)
        {
            if (namedArgument.Key == "Sortable" && namedArgument.Value.Value is bool sortableValue)
            {
                return sortableValue;
            }
        }
        return false;
    }


    private static void ExtractInterceptorMethod(
        IMethodSymbol methodSymbol,
        AttributeData interceptAttribute,
        List<DiagnosticInfo> diagnostics,
        List<InterceptorModel> interceptors,
        Dictionary<string, Location?> interceptedPropertyFirstLocation)
    {
        var propertyName = ReadConstructorString(interceptAttribute, position: 0);
        if (string.IsNullOrEmpty(propertyName)) return;

        var raw = false;
        foreach (var namedArgument in interceptAttribute.NamedArguments)
        {
            if (namedArgument.Key == "Raw" && namedArgument.Value.Value is bool rawValue)
            {
                raw = rawValue;
            }
        }

        var currentLocation = methodSymbol.Locations.FirstOrDefault();
        if (interceptedPropertyFirstLocation.TryGetValue(propertyName!, out var firstLocation))
        {
            var additionalLocations = firstLocation is not null
                ? new[] { firstLocation }
                : Array.Empty<Location>();
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.DuplicateInterceptor,
                currentLocation,
                additionalLocations,
                propertyName!));
            return;
        }
        interceptedPropertyFirstLocation[propertyName!] = currentLocation;

        // ValueClrType is null when the interceptor has fewer than two parameters (malformed);
        // skipping the wrapper is safer than fabricating "object" and producing wrong code.
        var valueClrType = methodSymbol.Parameters.Length >= 2
            ? methodSymbol.Parameters[1].Type.ToDisplayString()
            : null;

        interceptors.Add(new InterceptorModel(
            PropertyName: propertyName!,
            MethodName: methodSymbol.Name,
            Raw: raw,
            ValueClrType: valueClrType));
    }

    private static void ExtractPropertyOverrideMethod(
        IMethodSymbol methodSymbol,
        AttributeData propertyMapAttribute,
        Compilation compilation,
        List<PropertyOverrideModel> overrides,
        Dictionary<string, Location?> propertyMapFirstLocation)
    {
        var propertyName = ReadConstructorString(propertyMapAttribute, position: 0);
        if (string.IsNullOrEmpty(propertyName)) return;

        if (!propertyMapFirstLocation.ContainsKey(propertyName!))
        {
            propertyMapFirstLocation[propertyName!] = methodSymbol.Locations.FirstOrDefault();
        }

        overrides.Add(PropertyMapOverrideExtractor.Extract(methodSymbol, propertyName!, compilation));
    }

    private static (int DefaultPageSize, int MaxPageSize) ResolvePageSettings(
        INamedTypeSymbol classSymbol,
        IAssemblySymbol assemblySymbol)
    {
        var defaultPageSize = FallbackDefaultPageSize;
        var maxPageSize = FallbackMaxPageSize;

        foreach (var assemblyAttribute in assemblySymbol.GetAttributes())
        {
            if (assemblyAttribute.AttributeClass?.ToDisplayString() != FilterDefaultsAttributeFullName) continue;
            foreach (var namedArgument in assemblyAttribute.NamedArguments)
            {
                if (namedArgument.Key == "DefaultPageSize" && namedArgument.Value.Value is int dps)
                {
                    defaultPageSize = dps;
                }
                else if (namedArgument.Key == "MaxPageSize" && namedArgument.Value.Value is int mps)
                {
                    maxPageSize = mps;
                }
            }
        }

        foreach (var classAttribute in classSymbol.GetAttributes())
        {
            if (classAttribute.AttributeClass?.ToDisplayString() != PageSettingsAttributeFullName) continue;
            foreach (var namedArgument in classAttribute.NamedArguments)
            {
                if (namedArgument.Key == "DefaultPageSize" && namedArgument.Value.Value is int dps)
                {
                    defaultPageSize = dps;
                }
                else if (namedArgument.Key == "MaxPageSize" && namedArgument.Value.Value is int mps)
                {
                    maxPageSize = mps;
                }
            }
        }

        return (defaultPageSize, maxPageSize);
    }

    private static AttributeData? FindAttribute(System.Collections.Immutable.ImmutableArray<AttributeData> attributes, string fullName)
    {
        foreach (var attributeData in attributes)
        {
            if (attributeData.AttributeClass?.ToDisplayString() == fullName) return attributeData;
        }
        return null;
    }

    private static bool IsPartial(IMethodSymbol methodSymbol)
    {
        // Roslyn exposes both partial halves via the same symbol; either half may carry the modifier.
        foreach (var syntaxReference in methodSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is MethodDeclarationSyntax methodDeclaration)
            {
                if (methodDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static string? ReadConstructorString(AttributeData attributeData, int position)
    {
        if (attributeData.ConstructorArguments.Length <= position) return null;
        return attributeData.ConstructorArguments[position].Value as string;
    }
}
