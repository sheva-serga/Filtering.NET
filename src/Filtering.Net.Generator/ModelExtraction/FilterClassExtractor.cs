using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator;

// Phase one of filter-class extraction: everything the class's own syntax tree says. Nothing here
// reads compilation-global state, so Roslyn's per-tree caching of this transform cannot serve a
// model that went stale because the change landed in another file. FilterClassResolver finishes the
// job once the GeneratorIndex is combined in.
internal static class FilterClassExtractor
{
    private const string MapAttributeFullName = "Filtering.Net.MapAttribute";
    private const string InterceptValueAttributeFullName = "Filtering.Net.InterceptValueAttribute";
    private const string PropertyMapAttributeFullName = "Filtering.Net.PropertyMapAttribute";
    private const string PageSettingsAttributeFullName = "Filtering.Net.PageSettingsAttribute";
    private const string InterceptContextFullName = "Filtering.Net.InterceptContext";
    private const string JsonElementFullName = "System.Text.Json.JsonElement";

    public static FilterClassDeclarationWithDiagnostics Extract(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var diagnostics = new List<DiagnosticInfo>();

        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return new FilterClassDeclarationWithDiagnostics(Declaration: null, Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
        }

        var attributeData = context.Attributes.FirstOrDefault();
        if (attributeData?.AttributeClass is null || attributeData.AttributeClass.TypeArguments.Length != 1)
        {
            return new FilterClassDeclarationWithDiagnostics(Declaration: null, Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
        }

        if (attributeData.AttributeClass.TypeArguments[0] is not INamedTypeSymbol entityType)
        {
            return new FilterClassDeclarationWithDiagnostics(Declaration: null, Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // FN0027: the generated half is a top-level partial in the class's namespace, so a nested or
        // generic declaration would get a phantom partner instead of its base class and constructors.
        if (DescribePlacementProblem(classSymbol) is { } placementProblem)
        {
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.FilterClassPlacementInvalid,
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name,
                placementProblem));
            return new FilterClassDeclarationWithDiagnostics(Declaration: null, Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
        }

        // FN0020: the generated part declares FilterDefinition<TEntity> as the base class.
        if (classSymbol.BaseType is { SpecialType: not SpecialType.System_Object } declaredBaseType)
        {
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.FilterClassHasBaseType,
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name,
                declaredBaseType.ToDisplayString()));
            return new FilterClassDeclarationWithDiagnostics(Declaration: null, Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
        }

        var (classDefaultPageSize, classMaxPageSize) = ReadClassPageSettings(classSymbol);

        // -------- Walk declarations --------
        var properties = new List<PropertyDeclarationModel>();
        var interceptors = new List<InterceptorModel>();
        var overrides = new List<PropertyOverrideModel>();

        var mappedPropertyFirstLocation = new Dictionary<string, Location?>(StringComparer.Ordinal);
        var interceptedPropertyFirstLocation = new Dictionary<string, Location?>(StringComparer.Ordinal);
        var propertyMapFirstLocation = new Dictionary<string, Location?>(StringComparer.Ordinal);

        // [Map] is declared on the class, once per property.
        foreach (var classAttribute in classSymbol.GetAttributes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (classAttribute.AttributeClass?.ToDisplayString() != MapAttributeFullName) continue;
            ExtractMapAttribute(
                classSymbol,
                entityType,
                classAttribute,
                diagnostics,
                properties,
                mappedPropertyFirstLocation);
        }

        // [InterceptValue] and [PropertyMap] stay on methods because those methods have bodies.
        foreach (var member in classSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is not IMethodSymbol methodSymbol) continue;

            var memberAttributes = methodSymbol.GetAttributes();
            var interceptAttribute = FindAttribute(memberAttributes, InterceptValueAttributeFullName);
            var propertyMapAttribute = FindAttribute(memberAttributes, PropertyMapAttributeFullName);

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
                    classSymbol,
                    methodSymbol,
                    propertyMapAttribute,
                    context.SemanticModel,
                    diagnostics,
                    overrides,
                    propertyMapFirstLocation);
            }
        }

        // -------- Cross-member validations --------

        // FN0002: same property name appearing in both [Map] and [PropertyMap].
        // Primary squiggle on the [PropertyMap] site (more naturally available — it's the override
        // shadowing the regular [Map]); additional points at the colliding [Map] method.
        var shadowedByMap = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sharedPropertyName in mappedPropertyFirstLocation.Keys.Intersect(propertyMapFirstLocation.Keys, StringComparer.Ordinal))
        {
            shadowedByMap.Add(sharedPropertyName);
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

        // FN0023: an override the generated CreateSchema cannot call would otherwise vanish from the
        // schema without a word. A name already shadowed by a [Map] is FN0002's business.
        foreach (var propertyOverride in overrides)
        {
            if (propertyOverride.SignatureProblem is null) continue;
            if (shadowedByMap.Contains(propertyOverride.PropertyName)) continue;
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.PropertyMapSignatureInvalid,
                propertyOverride.DeclarationLocation,
                propertyOverride.PropertyName,
                propertyOverride.MethodName,
                propertyOverride.SignatureProblem));
        }

        foreach (var interceptedEntry in interceptedPropertyFirstLocation)
        {
            if (!mappedPropertyFirstLocation.ContainsKey(interceptedEntry.Key))
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

        var nestedMappings = MapNestedExtractor.Extract(classSymbol, entityType, diagnostics, cancellationToken);

        var declaration = new FilterClassDeclaration(
            Namespace: classNamespace,
            ClassName: classSymbol.Name,
            FullEntityTypeName: entityType.ToDisplayString(),
            ClassDefaultPageSize: classDefaultPageSize,
            ClassMaxPageSize: classMaxPageSize,
            Properties: new EquatableList<PropertyDeclarationModel>(properties),
            Interceptors: new EquatableList<InterceptorModel>(interceptors),
            Overrides: new EquatableList<PropertyOverrideModel>(overrides),
            Location: LocationInfo.FromLocation(classSymbol.Locations.FirstOrDefault()),
            NestedMappings: nestedMappings);

        return new FilterClassDeclarationWithDiagnostics(
            Declaration: declaration,
            Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
    }

    // Returns null when the class can carry a generated top-level partial, else why it cannot.
    private static string? DescribePlacementProblem(INamedTypeSymbol classSymbol)
    {
        var problems = new List<string>(2);
        if (classSymbol.ContainingType is { } containingType)
        {
            problems.Add($"it is nested in '{containingType.ToDisplayString()}'");
        }
        if (classSymbol.TypeParameters.Length > 0)
        {
            problems.Add("it declares type parameters");
        }
        return problems.Count == 0 ? null : string.Join(" and ", problems);
    }

    private static void ExtractMapAttribute(
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol entityType,
        AttributeData mapAttribute,
        List<DiagnosticInfo> diagnostics,
        List<PropertyDeclarationModel> properties,
        Dictionary<string, Location?> mappedPropertyFirstLocation)
    {
        var extractionResult = PropertyMappingExtractor.ExtractDeclaration(entityType, mapAttribute);
        diagnostics.AddRange(extractionResult.Diagnostics);

        // A failed extraction still claims its name so a second [Map] for the same name fires FN0001.
        var propertyName = extractionResult.Model?.PropertyName ?? ReadConstructorString(mapAttribute, position: 0);
        if (string.IsNullOrEmpty(propertyName)) return;

        var attributeLocation = mapAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
        if (mappedPropertyFirstLocation.TryGetValue(propertyName!, out var previousLocation))
        {
            diagnostics.Add(DuplicateMapping(
                classSymbol,
                attributeLocation,
                previousLocation,
                propertyName!,
                "[Map] " + propertyName + ", [Map] " + propertyName));
            return;
        }

        mappedPropertyFirstLocation[propertyName!] = attributeLocation;
        if (extractionResult.Model is not null) properties.Add(extractionResult.Model);
    }

    private static DiagnosticInfo DuplicateMapping(
        INamedTypeSymbol classSymbol,
        Location? currentLocation,
        Location? previousLocation,
        string mappedPath,
        string sourcesFormatted)
    {
        var hostFqn = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        var additionalLocations = previousLocation is not null
            ? new[] { previousLocation }
            : Array.Empty<Location>();
        return DiagnosticInfo.From(
            DiagnosticDescriptors.DuplicateMapping,
            currentLocation,
            additionalLocations,
            mappedPath,
            hostFqn,
            sourcesFormatted);
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

        // FN0029: the method group is spliced straight into the static CreateSchema, so a shape the
        // builder overloads cannot take is a compile error inside generated code or a silent drop.
        var signatureProblem = DescribeInterceptorSignatureProblem(methodSymbol, raw);
        if (signatureProblem is not null)
        {
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.InterceptorSignatureInvalid,
                currentLocation,
                propertyName!,
                methodSymbol.Name,
                signatureProblem));
            return;
        }

        interceptors.Add(new InterceptorModel(
            PropertyName: propertyName!,
            MethodName: methodSymbol.Name,
            Raw: raw,
            ValueClrType: methodSymbol.Parameters[1].Type.ToDisplayString()));
    }

    // Returns null when the generated CreateSchema can pass the method group to Intercept /
    // InterceptArray / InterceptRaw, else why it cannot.
    private static string? DescribeInterceptorSignatureProblem(IMethodSymbol methodSymbol, bool raw)
    {
        if (!methodSymbol.IsStatic) return "it is an instance method";
        if (methodSymbol.Parameters.Length != 2)
        {
            return $"it takes {methodSymbol.Parameters.Length} parameters instead of exactly two (InterceptContext, value)";
        }

        var contextParameterType = methodSymbol.Parameters[0].Type.ToDisplayString();
        if (contextParameterType != InterceptContextFullName)
        {
            return $"its first parameter is '{contextParameterType}' instead of InterceptContext";
        }

        var valueParameterType = methodSymbol.Parameters[1].Type.ToDisplayString();
        if (raw)
        {
            return valueParameterType == JsonElementFullName
                ? null
                : $"Raw = true requires a System.Text.Json.JsonElement second parameter, not '{valueParameterType}'";
        }

        var returnTypeName = methodSymbol.ReturnType.ToDisplayString();
        return returnTypeName == valueParameterType
            ? null
            : $"it takes a '{valueParameterType}' but returns '{returnTypeName}'; a non-raw interceptor returns the type it receives";
    }

    private static void ExtractPropertyOverrideMethod(
        INamedTypeSymbol classSymbol,
        IMethodSymbol methodSymbol,
        AttributeData propertyMapAttribute,
        SemanticModel? semanticModel,
        List<DiagnosticInfo> diagnostics,
        List<PropertyOverrideModel> overrides,
        Dictionary<string, Location?> propertyMapFirstLocation)
    {
        var propertyName = ReadConstructorString(propertyMapAttribute, position: 0);
        if (string.IsNullOrEmpty(propertyName)) return;

        var attributeLocation = propertyMapAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
            ?? methodSymbol.Locations.FirstOrDefault();

        // FN0001: two rules for one path would both reach FilterSchema, which rejects the wire key
        // at construction. Keep the first so the rest of the class still models correctly.
        if (propertyMapFirstLocation.TryGetValue(propertyName!, out var firstLocation))
        {
            diagnostics.Add(DuplicateMapping(
                classSymbol,
                attributeLocation,
                firstLocation,
                propertyName!,
                "[PropertyMap] " + propertyName + ", [PropertyMap] " + propertyName));
            return;
        }

        propertyMapFirstLocation[propertyName!] = attributeLocation;

        overrides.Add(PropertyMapOverrideExtractor.Extract(
            methodSymbol,
            propertyName!,
            LocationInfo.FromLocation(attributeLocation),
            semanticModel));
    }

    private static (int? DefaultPageSize, int? MaxPageSize) ReadClassPageSettings(INamedTypeSymbol classSymbol)
    {
        int? defaultPageSize = null;
        int? maxPageSize = null;

        foreach (var classAttribute in classSymbol.GetAttributes())
        {
            if (classAttribute.AttributeClass?.ToDisplayString() != PageSettingsAttributeFullName) continue;
            foreach (var namedArgument in classAttribute.NamedArguments)
            {
                if (namedArgument.Key == "DefaultPageSize" && namedArgument.Value.Value is int configuredDefaultPageSize)
                {
                    defaultPageSize = configuredDefaultPageSize;
                }
                else if (namedArgument.Key == "MaxPageSize" && namedArgument.Value.Value is int configuredMaxPageSize)
                {
                    maxPageSize = configuredMaxPageSize;
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

    private static string? ReadConstructorString(AttributeData attributeData, int position)
    {
        if (attributeData.ConstructorArguments.Length <= position) return null;
        return attributeData.ConstructorArguments[position].Value as string;
    }
}
