using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filtering.Net.Generator;

/// <summary>
/// Roslyn incremental source generator that turns partial classes annotated with
/// <c>[GenerateFilter&lt;TEntity&gt;]</c> into typed filter implementations. Also walks
/// <c>[FilterProfile]</c>-marked classes (the second pipeline branch) to emit profile- and
/// operator-level diagnostics.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class FilterGenerator : IIncrementalGenerator
{
    private const string GenerateFilterAttributeFullName = "Filtering.Net.GenerateFilterAttribute`1";
    private const string FilterProfileAttributeFullName = "Filtering.Net.FilterProfileAttribute`1";

    /// <inheritdoc />
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

        // Task 15: per-enum auto-emitted profiles. Walks every enum referenced by any
        // [GenerateFilter<TEntity>] property and emits one [FilterProfile<TEnum>] class.
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
        // modelsCollected is already an ImmutableArray<FilterClassModel?> (nullable due to Where filtering);
        // use ! to match the non-nullable parameter signature, consistent with GenerateAssemblyDiExtension and GenerateEnumProfiles.
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

    /// <summary>
    /// Cross-class checks. Runs once both pipeline branches have produced their per-class output.
    /// FN1003 (ProfileUnused) — a [FilterProfile] type that no [Map(... Profile = typeof(X))] cites.
    /// FN1004 (OperatorUnused) — an operator on such a profile that is never named in any
    /// <c>Only =</c> list anywhere. Conservative version: an operator implicitly included by the
    /// absence of <c>Only=</c> doesn't count as a reference, so this rule only fires when
    /// <em>every</em> consumer of the profile excludes the operator with an <c>Only=</c> list.
    /// </summary>
    private static void ReportCrossPipelineDiagnostics(
        SourceProductionContext sourceProductionContext,
        (ImmutableArray<FilterClassModelWithDiagnostics> Filters, ImmutableArray<ProfileModelWithDiagnostics> Profiles) bundle)
    {
        var (filterClassResults, profileResults) = bundle;

        // Index profile usages from filter-class models.
        var profilesReferenced = new HashSet<string>(StringComparer.Ordinal);
        // operatorReferences: for each (profileName, operatorName) we track whether it's been
        // explicitly used (Only=) on at least one mapping. If a mapping doesn't supply Only=, all
        // operators count as implicitly used and we skip FN1004 for the whole profile.
        var profilesWithImplicitFullUsage = new HashSet<string>(StringComparer.Ordinal);
        var explicitlyReferencedOperators = new HashSet<string>(StringComparer.Ordinal);

        foreach (var filterResult in filterClassResults)
        {
            if (filterResult.Model is null) continue;
            foreach (var mapping in filterResult.Model.Properties)
            {
                profilesReferenced.Add(mapping.ProfileFullName);
                // AllowedOperators = profile operators filtered by Only/Except. If AllowedOperators
                // == profile operators (no narrowing), treat as implicit-full-usage. Otherwise the
                // listed operators count as explicit references.
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

            // FN1004: operator on a used profile is never referenced.
            // Conservative interpretation: an operator counts as "referenced" when it survives
            // the Only/Except filter on at least one mapping that uses this profile. Since a
            // mapping without an Only=/Except= retains every profile operator in AllowedOperators,
            // FN1004 only fires when every single consumer of the profile excluded the operator
            // explicitly via Only= or Except=.
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

    /// <summary>Emits one <c>FilteringServiceCollectionExtensions.AddFiltering</c> per assembly
    /// that lists every discovered filter class. Skipped silently when the consumer project
    /// doesn't reference Microsoft.Extensions.DependencyInjection.Abstractions.</summary>
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

    /// <summary>
    /// FN1008: when the assembly opts in via <c>[assembly: FilterValueDiagnostics(WarnUnregistered = true)]</c>,
    /// emits one diagnostic per typed-value type that is not registered in any
    /// <c>[JsonSerializable(typeof(T))]</c> attribute on a <c>JsonSerializerContext</c> visible in
    /// the compilation. Silently returns without emitting when the opt-in is absent.
    /// </summary>
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
                // Try both with and without the global:: prefix — the stored CLR type string
                // format may differ from SymbolDisplayFormat.FullyQualifiedFormat's output.
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

    /// <summary>Task 15: emit one <c>[FilterProfile&lt;TEnum&gt;]</c> static class per enum
    /// referenced by any <c>[GenerateFilter&lt;TEntity&gt;]</c> property. Skipped silently when
    /// no filter classes were discovered.</summary>
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
