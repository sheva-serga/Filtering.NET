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

        // mappedPropertySortable.Value tracks whether the prior [Map] had Sortable=true — needed
        // to pick FN0001 (DuplicateMap) vs FN0002 (DuplicateSortable) on a second declaration.
        var mappedPropertySortable = new Dictionary<string, bool>(StringComparer.Ordinal);
        var interceptedPropertyNames = new HashSet<string>(StringComparer.Ordinal);
        var propertyMapNames = new HashSet<string>(StringComparer.Ordinal);

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
                    entityType,
                    mapAttribute,
                    compilation,
                    profileIndex,
                    diagnostics,
                    properties,
                    mappedPropertySortable);
            }

            if (interceptAttribute is not null)
            {
                ExtractInterceptorMethod(
                    methodSymbol,
                    interceptAttribute,
                    diagnostics,
                    interceptors,
                    interceptedPropertyNames);
            }

            if (propertyMapAttribute is not null)
            {
                ExtractPropertyOverrideMethod(
                    methodSymbol,
                    propertyMapAttribute,
                    compilation,
                    overrides,
                    propertyMapNames);
            }
        }

        // -------- Cross-method validations --------

        // FN0003: Property name appearing in both [Map] and [PropertyMap].
        foreach (var sharedPropertyName in mappedPropertySortable.Keys.Intersect(propertyMapNames, StringComparer.Ordinal))
        {
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.MapAndPropertyMapBoth,
                classSymbol.Locations.FirstOrDefault(),
                sharedPropertyName));
        }

        // FN0011: aliases must not collide (case-insensitively) with property names or other aliases.
        DetectAliasCollisions(properties, classSymbol, entityType, diagnostics);

        // FN1002: numeric/date property mapped without Sortable=true.
        DetectMissingSortable(properties, classSymbol, diagnostics);

        // FN0013: [InterceptValue] without a matching [Map].
        foreach (var interceptedName in interceptedPropertyNames)
        {
            if (!mappedPropertySortable.ContainsKey(interceptedName))
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.InterceptorWithoutMap,
                    classSymbol.Locations.FirstOrDefault(),
                    interceptedName));
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
            HasAnyTypedValueProperty: hasAnyTypedValueProperty);

        return new FilterClassModelWithDiagnostics(
            Model: model,
            Diagnostics: new EquatableList<DiagnosticInfo>(diagnostics));
    }

    private static void ExtractMapMethod(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol entityType,
        AttributeData mapAttribute,
        Compilation compilation,
        ProfileIndex profileIndex,
        List<DiagnosticInfo> diagnostics,
        List<PropertyMappingModel> properties,
        Dictionary<string, bool> mappedPropertySortable)
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
            // Still record the property name so a second [Map] for the same name fires FN0001.
            var attemptedName = ReadConstructorString(mapAttribute, position: 0);
            if (!string.IsNullOrEmpty(attemptedName))
            {
                EmitDuplicateMapDiagnosticIfNeeded(
                    methodSymbol,
                    attemptedName!,
                    sortableOnThisMap: ReadSortableNamedArg(mapAttribute),
                    mappedPropertySortable,
                    diagnostics);
            }
            return;
        }

        var modelSortable = extractionResult.Model.Sortable;
        if (mappedPropertySortable.ContainsKey(extractionResult.Model.PropertyName))
        {
            EmitDuplicateMapDiagnosticIfNeeded(
                methodSymbol,
                extractionResult.Model.PropertyName,
                sortableOnThisMap: modelSortable,
                mappedPropertySortable,
                diagnostics);
            return;
        }

        mappedPropertySortable[extractionResult.Model.PropertyName] = modelSortable;
        properties.Add(extractionResult.Model);
    }

    // Picks FN0002 (DuplicateSortable) when both the prior and current mapping are Sortable=true;
    // otherwise emits the more general FN0001 (DuplicateMap).
    private static void EmitDuplicateMapDiagnosticIfNeeded(
        IMethodSymbol methodSymbol,
        string propertyName,
        bool sortableOnThisMap,
        Dictionary<string, bool> mappedPropertySortable,
        List<DiagnosticInfo> diagnostics)
    {
        var location = methodSymbol.Locations.FirstOrDefault();
        if (mappedPropertySortable.TryGetValue(propertyName, out var sortableAlreadySeen))
        {
            if (sortableAlreadySeen && sortableOnThisMap)
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.DuplicateSortable,
                    location,
                    propertyName));
                return;
            }
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.DuplicateMap,
                location,
                propertyName));
            return;
        }
        mappedPropertySortable[propertyName] = sortableOnThisMap;
    }

    private static void DetectAliasCollisions(
        List<PropertyMappingModel> properties,
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol entityType,
        List<DiagnosticInfo> diagnostics)
    {
        var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in properties)
        {
            existingNames.Add(mapping.PropertyName);
        }
        foreach (var mapping in properties)
        {
            if (string.IsNullOrEmpty(mapping.Alias)) continue;
            if (!existingNames.Add(mapping.Alias!))
            {
                diagnostics.Add(DiagnosticInfo.From(
                    DiagnosticDescriptors.AliasCollision,
                    classSymbol.Locations.FirstOrDefault(),
                    mapping.Alias!,
                    entityType.ToDisplayString()));
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
        HashSet<string> interceptedPropertyNames)
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

        if (!interceptedPropertyNames.Add(propertyName!))
        {
            diagnostics.Add(DiagnosticInfo.From(
                DiagnosticDescriptors.DuplicateInterceptor,
                methodSymbol.Locations.FirstOrDefault(),
                propertyName!));
            return;
        }

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
        HashSet<string> propertyMapNames)
    {
        var propertyName = ReadConstructorString(propertyMapAttribute, position: 0);
        if (string.IsNullOrEmpty(propertyName)) return;

        propertyMapNames.Add(propertyName!);

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
