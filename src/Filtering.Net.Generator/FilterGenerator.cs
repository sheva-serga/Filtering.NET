using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class FilterGenerator : IIncrementalGenerator
{
    private const string GenerateFilterAttributeFullName = "Filtering.Net.GenerateFilterAttribute`1";
    private const string FilterProfileAttributeFullName = "Filtering.Net.FilterProfileAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // -------- Pipeline branch 1: [GenerateFilter<TEntity>] partial classes --------
        var filterClasses = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenerateFilterAttributeFullName,
            predicate: static (syntaxNode, _) =>
                syntaxNode is ClassDeclarationSyntax classDeclaration
                && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword),
            transform: static (generatorContext, cancellationToken) =>
                ExtractFilterClassModel(generatorContext, cancellationToken))
            .WithTrackingName(TrackingNames.FilterClassModels);

        // Always report the per-class diagnostics (regardless of whether a model came back).
        context.RegisterSourceOutput(filterClasses, ReportFilterClassDiagnostics);

        var modelsForEmission = filterClasses
            .Select(static (extractionResult, _) => extractionResult.Model)
            .Where(static model => model is not null);
        context.RegisterSourceOutput(modelsForEmission, GenerateForFilterClass!);

        // Assembly-wide AddFiltering DI extension. Only emit when the consumer references
        // Microsoft.Extensions.DependencyInjection.Abstractions; otherwise the emitted call
        // to IServiceCollection wouldn't compile.
        var modelsCollected = modelsForEmission.Collect();
        var compilationProvider = context.CompilationProvider;
        var diBundle = modelsCollected.Combine(compilationProvider);
        context.RegisterSourceOutput(diBundle, GenerateAssemblyDiExtension!);

        var enumEmissionBundle = modelsCollected.Combine(compilationProvider);
        context.RegisterSourceOutput(enumEmissionBundle, GenerateEnumProfiles!);

        // -------- Pipeline branch 2: [FilterProfile] classes --------
        var profileClasses = context.SyntaxProvider.ForAttributeWithMetadataName(
            FilterProfileAttributeFullName,
            predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
            transform: static (generatorContext, cancellationToken) =>
                ProfileExtractor.Extract(generatorContext, cancellationToken));

        context.RegisterSourceOutput(profileClasses, ReportProfileDiagnostics);

        // -------- Cross-pipeline diagnostics (FN1003 ProfileUnused, FN1004 OperatorUnused) --------
        var filterClassesCollected = filterClasses.Collect();
        var profileClassesCollected = profileClasses.Collect();
        var combinedForCrossDiagnostics = filterClassesCollected.Combine(profileClassesCollected);
        context.RegisterSourceOutput(combinedForCrossDiagnostics, ReportCrossPipelineDiagnostics);

        // -------- FN1008: FilterValueTypeUnregistered (opt-in via [assembly: FilterValueDiagnostics(WarnUnregistered = true)]) --------
        var allModelsAndCompilation = modelsCollected.Combine(context.CompilationProvider);
        context.RegisterSourceOutput(allModelsAndCompilation, EmitFn1008IfOptedIn!);
    }

    private static FilterClassModelWithDiagnostics ExtractFilterClassModel(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        return FilterClassExtractor.Extract(context, cancellationToken);
    }

    private static void ReportFilterClassDiagnostics(SourceProductionContext sourceProductionContext, FilterClassModelWithDiagnostics extractionResult)
    {
        foreach (var diagnosticInfo in extractionResult.Diagnostics)
        {
            sourceProductionContext.ReportDiagnostic(diagnosticInfo.ToDiagnostic());
        }
    }

    private static void ReportProfileDiagnostics(SourceProductionContext sourceProductionContext, ProfileModelWithDiagnostics extractionResult)
    {
        foreach (var diagnosticInfo in extractionResult.Diagnostics)
        {
            sourceProductionContext.ReportDiagnostic(diagnosticInfo.ToDiagnostic());
        }
    }

    private static void ReportCrossPipelineDiagnostics(
        SourceProductionContext sourceProductionContext,
        (ImmutableArray<FilterClassModelWithDiagnostics> Filters, ImmutableArray<ProfileModelWithDiagnostics> Profiles) bundle)
    {
        var (filterClassResults, profileResults) = bundle;

        var profilesReferenced = new HashSet<string>(StringComparer.Ordinal);
        var profilesWithImplicitFullUsage = new HashSet<string>(StringComparer.Ordinal);
        var explicitlyReferencedOperators = new HashSet<string>(StringComparer.Ordinal);

        foreach (var filterResult in filterClassResults)
        {
            if (filterResult.Model is null) continue;
            foreach (var mapping in filterResult.Model.Properties)
            {
                profilesReferenced.Add(mapping.ProfileFullName);
                foreach (var operatorName in mapping.AllowedOperators)
                {
                    explicitlyReferencedOperators.Add($"{mapping.ProfileFullName}|{operatorName}");
                }
            }
        }

        foreach (var profileResult in profileResults)
        {
            if (profileResult.Model is null) continue;
            var profileFullName = profileResult.Model.ProfileFullName;

            // FN1003: profile declared but unused.
            if (!profilesReferenced.Contains(profileFullName))
            {
                var location = profileResult.Model.Location?.ToLocation() ?? Location.None;
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ProfileUnused,
                    location,
                    profileFullName));
                continue;
            }

            // FN1004: only fires when every consumer of the profile excluded the operator via Only=/Except=.
            foreach (var operatorName in profileResult.Model.OperatorNames)
            {
                var key = $"{profileFullName}|{operatorName}";
                if (explicitlyReferencedOperators.Contains(key)) continue;
                var location = profileResult.Model.Location?.ToLocation() ?? Location.None;
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.OperatorUnused,
                    location,
                    operatorName,
                    profileFullName));
            }
        }
    }

    private static void GenerateForFilterClass(SourceProductionContext sourceProductionContext, FilterClassModel model)
    {
        var emittedSource = SourceEmitter.EmitForClass(model);
        var hintName = string.IsNullOrEmpty(model.Namespace)
            ? $"{model.ClassName}.g.cs"
            : $"{model.Namespace}.{model.ClassName}.g.cs";
        sourceProductionContext.AddSource(hintName, emittedSource);
    }

    private static void GenerateAssemblyDiExtension(
        SourceProductionContext sourceProductionContext,
        (ImmutableArray<FilterClassModel> Models, Compilation Compilation) bundle)
    {
        var (models, compilation) = bundle;
        if (models.IsDefaultOrEmpty) return;
        if (!DiExtensionEmitter.IsDiAbstractionsReferenced(compilation)) return;

        var emittedSource = DiExtensionEmitter.Emit(models, compilation.AssemblyName ?? "GeneratedAssembly");
        sourceProductionContext.AddSource("FilteringServiceCollectionExtensions.g.cs", emittedSource);
    }

    private const string FilterValueDiagnosticsAttributeFullName = "Filtering.Net.FilterValueDiagnosticsAttribute";

    private static void EmitFn1008IfOptedIn(
        SourceProductionContext sourceProductionContext,
        (ImmutableArray<FilterClassModel> Models, Compilation Compilation) bundle)
    {
        var (models, compilation) = bundle;

        if (!IsWarnUnregisteredOptIn(compilation)) return;

        var registeredTypes = JsonSerializableTypeCollector.CollectRegisteredTypes(compilation);
        var registeredFullNames = new HashSet<string>(
            registeredTypes.Select(symbol => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            StringComparer.Ordinal);

        foreach (var model in models)
        {
            foreach (var typedValueReference in TypedValueTypeCollector.Collect(model))
            {
                // The stored CLR type string may or may not carry the global:: prefix.
                var clrTypeWithGlobal = typedValueReference.ValueClrType.StartsWith("global::")
                    ? typedValueReference.ValueClrType
                    : "global::" + typedValueReference.ValueClrType;
                var clrTypeWithoutGlobal = typedValueReference.ValueClrType.StartsWith("global::")
                    ? typedValueReference.ValueClrType.Substring("global::".Length)
                    : typedValueReference.ValueClrType;

                if (registeredFullNames.Contains(clrTypeWithGlobal) ||
                    registeredFullNames.Contains(clrTypeWithoutGlobal))
                {
                    continue;
                }

                var location = typedValueReference.Location?.ToLocation() ?? Location.None;
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FilterValueTypeUnregistered,
                    location,
                    typedValueReference.ValueClrType,
                    typedValueReference.OwnerLabel));
            }
        }
    }

    private static bool IsWarnUnregisteredOptIn(Compilation compilation)
    {
        var attributeSymbol = compilation.GetTypeByMetadataName(FilterValueDiagnosticsAttributeFullName);
        if (attributeSymbol is null) return false;

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol)) continue;

            foreach (var namedArgument in attribute.NamedArguments)
            {
                if (namedArgument.Key == "WarnUnregistered" &&
                    namedArgument.Value.Value is bool warnUnregistered)
                {
                    return warnUnregistered;
                }
            }
        }
        return false;
    }

    private static void GenerateEnumProfiles(
        SourceProductionContext sourceProductionContext,
        (ImmutableArray<FilterClassModel> Models, Compilation Compilation) bundle)
    {
        var (models, compilation) = bundle;
        if (models.IsDefaultOrEmpty) return;
        var enumTypes = EnumTypeCollector.Collect(compilation);
        foreach (var enumSymbol in enumTypes)
        {
            var emittedSource = EnumProfileEmitter.Emit(enumSymbol);
            var hintName = $"{EnumProfileEmitter.GeneratedNamespace}.{enumSymbol.Name}Filter.g.cs";
            sourceProductionContext.AddSource(hintName, emittedSource);
        }
    }
}
