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
        // -------- Compilation-wide index --------
        // Profiles, enum profiles and [assembly: FilterDefaults] are read once here instead of once
        // per filter class, and every consumer of them is combined with this node, so a change in
        // one file cannot leave another file's cached model behind.
        var generatorIndex = context.CompilationProvider
            .Select(static (compilation, cancellationToken) => GeneratorIndexBuilder.Build(compilation, cancellationToken))
            .WithTrackingName(TrackingNames.GeneratorIndex);

        // -------- Pipeline branch 1: [GenerateFilter<TEntity>] partial classes --------
        var filterClassDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenerateFilterAttributeFullName,
            predicate: static (syntaxNode, _) =>
                syntaxNode is ClassDeclarationSyntax classDeclaration
                && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword),
            transform: static (generatorContext, cancellationToken) =>
                FilterClassExtractor.Extract(generatorContext, cancellationToken))
            .WithTrackingName(TrackingNames.FilterClassDeclarations);

        var filterClasses = filterClassDeclarations
            .Combine(generatorIndex)
            .Select(static (input, cancellationToken) =>
                FilterClassResolver.Resolve(input.Left, input.Right, cancellationToken))
            .WithTrackingName(TrackingNames.FilterClassModels);

        // Always report the per-class diagnostics (regardless of whether a model came back).
        context.RegisterSourceOutput(filterClasses, ReportFilterClassDiagnostics);

        // [MapNested] splice runs after extraction; it needs every host's model and nothing else,
        // because each navigation was already classified against its entity during extraction.
        var allHostsCollected = filterClasses.Collect();
        var resolvedHosts = filterClasses
            .Combine(allHostsCollected)
            .Select(static (input, cancellationToken) =>
                NestedFilterResolver.Resolve(input.Left, input.Right, cancellationToken))
            .WithTrackingName(TrackingNames.ResolvedFilterClassModels);

        context.RegisterSourceOutput(resolvedHosts, ReportResolvedDiagnostics);

        var modelsForEmission = resolvedHosts
            .Select(static (resolved, _) => resolved.Model)
            .Where(static model => model is not null);
        context.RegisterSourceOutput(modelsForEmission, GenerateForFilterClass!);

        // Assembly-wide AddFiltering DI extension. Only emit when the consumer references
        // Microsoft.Extensions.DependencyInjection.Abstractions; otherwise the emitted call
        // to IServiceCollection wouldn't compile.
        var modelsCollected = modelsForEmission.Collect();
        var diBundle = modelsCollected.Combine(generatorIndex);
        context.RegisterSourceOutput(diBundle, GenerateAssemblyDiExtension!);

        context.RegisterSourceOutput(modelsCollected, GenerateProfileBridges);

        var enumEmissionBundle = modelsCollected.Combine(generatorIndex);
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
        // The opt-in flag and the registered type names ride on the index, so this node re-runs only
        // when one of them actually changed rather than on every Compilation instance.
        var fn1008Bundle = modelsCollected.Combine(generatorIndex);
        context.RegisterSourceOutput(fn1008Bundle, EmitFn1008IfOptedIn!);
    }

    private static void ReportFilterClassDiagnostics(SourceProductionContext sourceProductionContext, FilterClassModelWithDiagnostics extractionResult)
    {
        foreach (var diagnosticInfo in extractionResult.Diagnostics)
        {
            sourceProductionContext.ReportDiagnostic(diagnosticInfo.ToDiagnostic());
        }
    }

    private static void ReportResolvedDiagnostics(SourceProductionContext sourceProductionContext, NestedFilterResolver.ResolvedHost resolvedHost)
    {
        foreach (var diagnosticInfo in resolvedHost.Diagnostics)
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
        var explicitlyReferencedOperators = new HashSet<string>(StringComparer.Ordinal);

        foreach (var filterResult in filterClassResults)
        {
            if (filterResult.Model is null) continue;
            foreach (var mapping in filterResult.Model.Properties)
            {
                // The whole BasedOn chain counts as referenced: every bridge in it is emitted and
                // its operators are inherited by the leaf profile the [Map] actually names.
                profilesReferenced.Add(mapping.ProfileFullName);
                foreach (var bridge in mapping.ProfileBridges)
                {
                    profilesReferenced.Add(bridge.ProfileFullName);
                }

                foreach (var operatorName in mapping.AllowedOperators)
                {
                    explicitlyReferencedOperators.Add($"{mapping.ProfileFullName}|{operatorName}");
                    foreach (var bridge in mapping.ProfileBridges)
                    {
                        explicitlyReferencedOperators.Add($"{bridge.ProfileFullName}|{operatorName}");
                    }
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

    private static void GenerateProfileBridges(SourceProductionContext sourceProductionContext, ImmutableArray<FilterClassModel> models)
    {
        if (models.IsDefaultOrEmpty) return;
        var profileBridges = ProfileBridgeEmitter.CollectDistinct(models);
        if (profileBridges.Count == 0) return;
        sourceProductionContext.AddSource(ProfileBridgeEmitter.HintName, ProfileBridgeEmitter.Emit(profileBridges));
    }

    private static void GenerateAssemblyDiExtension(
        SourceProductionContext sourceProductionContext,
        (ImmutableArray<FilterClassModel> Models, GeneratorIndex Index) bundle)
    {
        var (models, index) = bundle;
        if (models.IsDefaultOrEmpty) return;
        if (!index.IsDependencyInjectionReferenced) return;

        var emittedSource = DiExtensionEmitter.Emit(models);
        sourceProductionContext.AddSource("FilteringServiceCollectionExtensions.g.cs", emittedSource);
    }

    private static void EmitFn1008IfOptedIn(
        SourceProductionContext sourceProductionContext,
        (ImmutableArray<FilterClassModel> Models, GeneratorIndex Index) bundle)
    {
        var (models, index) = bundle;

        if (!index.IsFilterValueDiagnosticsOptIn) return;

        // The index stores every registration with and without the global:: prefix, so the stored
        // ValueClrType matches whichever spelling it happens to carry.
        var registeredFullNames = new HashSet<string>(index.RegisteredJsonTypeNames, StringComparer.Ordinal);

        foreach (var model in models)
        {
            foreach (var typedValueReference in TypedValueTypeCollector.Collect(model))
            {
                if (registeredFullNames.Contains(typedValueReference.ValueClrType)) continue;

                var location = typedValueReference.Location?.ToLocation() ?? Location.None;
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FilterValueTypeUnregistered,
                    location,
                    typedValueReference.ValueClrType,
                    typedValueReference.OwnerLabel));
            }
        }
    }

    private static void GenerateEnumProfiles(
        SourceProductionContext sourceProductionContext,
        (ImmutableArray<FilterClassModel> Models, GeneratorIndex Index) bundle)
    {
        var (models, index) = bundle;
        if (models.IsDefaultOrEmpty) return;
        foreach (var enumProfile in index.EnumProfiles)
        {
            var emittedSource = EnumProfileEmitter.Emit(enumProfile);
            var hintName = $"{EnumProfileEmitter.GeneratedNamespace}.{enumProfile.ClassName}.g.cs";
            sourceProductionContext.AddSource(hintName, emittedSource);
        }
    }
}
