using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Filtering.Net.Generator.Tests;

internal static class GeneratorRunner
{
    public static GeneratorDriver RunDriver(
        string sourceCode,
        bool excludeDiAbstractions = true,
        bool excludeEntityFrameworkCore = false)
    {
        var compilation = BuildCompilation(sourceCode, excludeDiAbstractions, excludeEntityFrameworkCore);
        return CSharpGeneratorDriver.Create(new FilterGenerator()).RunGenerators(compilation);
    }

    /// <summary>
    /// Runs the generator against <paramref name="sourceCode"/> with incremental step tracking
    /// enabled and returns the <see cref="FilterClassModel"/> values produced by the
    /// <c>[GenerateFilter&lt;TEntity&gt;]</c> pipeline branch. The list is empty when the source
    /// contains no valid filter-class declarations.
    /// </summary>
    public static IReadOnlyList<FilterClassModel> ExtractFilterClassModels(
        string sourceCode,
        bool excludeDiAbstractions = true,
        bool excludeEntityFrameworkCore = false)
    {
        var extractionResults = ExtractFilterClassResults(sourceCode, excludeDiAbstractions, excludeEntityFrameworkCore);
        var models = new List<FilterClassModel>();
        foreach (var extractionResult in extractionResults)
        {
            if (extractionResult.Model is { } model)
                models.Add(model);
        }
        return models;
    }

    public static IReadOnlyList<FilterClassModelWithDiagnostics> ExtractFilterClassResults(
        string sourceCode,
        bool excludeDiAbstractions = true,
        bool excludeEntityFrameworkCore = false)
    {
        var compilation = BuildCompilation(sourceCode, excludeDiAbstractions, excludeEntityFrameworkCore);
        var driverOptions = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);
        var generator = new FilterGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: new ISourceGenerator[] { generator.AsSourceGenerator() },
            additionalTexts: null,
            parseOptions: null,
            optionsProvider: null,
            driverOptions: driverOptions);

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var extractionResults = new List<FilterClassModelWithDiagnostics>();
        foreach (var generatorResult in runResult.Results)
        {
            if (!generatorResult.TrackedSteps.TryGetValue(TrackingNames.FilterClassModels, out var steps))
                continue;
            foreach (var step in steps)
            {
                foreach (var (value, _) in step.Outputs)
                {
                    if (value is FilterClassModelWithDiagnostics extractionResult)
                        extractionResults.Add(extractionResult);
                }
            }
        }
        return extractionResults;
    }

    public static (GeneratorDriverRunResult RunResult, Compilation UpdatedCompilation) RunAndUpdate(
        string sourceCode,
        bool excludeDiAbstractions = true,
        bool excludeEntityFrameworkCore = false)
    {
        var compilation = BuildCompilation(sourceCode, excludeDiAbstractions, excludeEntityFrameworkCore);
        var driver = (CSharpGeneratorDriver)CSharpGeneratorDriver.Create(new FilterGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _);
        return (driver.GetRunResult(), updated);
    }

    internal static CSharpCompilation BuildCompilation(
        string sourceCode,
        bool excludeDiAbstractions = true,
        bool excludeEntityFrameworkCore = false) =>
        BuildCompilation([sourceCode], excludeDiAbstractions, excludeEntityFrameworkCore);

    internal static CSharpCompilation BuildCompilation(
        IReadOnlyList<string> sourceFiles,
        bool excludeDiAbstractions = true,
        bool excludeEntityFrameworkCore = false)
    {
        var syntaxTrees = sourceFiles.Select(sourceFile => CSharpSyntaxTree.ParseText(sourceFile)).ToArray();

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: syntaxTrees,
            references: ResolveReferences(excludeDiAbstractions, excludeEntityFrameworkCore),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    // Every test method builds at least one compilation, and re-reading the whole trusted-platform
    // list from disk each time also denies Roslyn any metadata reuse across them, because each
    // PortableExecutableReference carries its own AssemblyMetadata. Three flag combinations are used,
    // so one cached array per combination covers the suite.
    private static readonly Lazy<ImmutableArray<MetadataReference>> AllReferences =
        new(() => BuildReferenceSet(excludeDiAbstractions: false, excludeEntityFrameworkCore: false));

    private static readonly Lazy<ImmutableArray<MetadataReference>> ReferencesWithoutDiAbstractions =
        new(() => BuildReferenceSet(excludeDiAbstractions: true, excludeEntityFrameworkCore: false));

    private static readonly Lazy<ImmutableArray<MetadataReference>> ReferencesWithoutDiAbstractionsAndEntityFrameworkCore =
        new(() => BuildReferenceSet(excludeDiAbstractions: true, excludeEntityFrameworkCore: true));

    private static readonly Lazy<ImmutableArray<MetadataReference>> ReferencesWithoutEntityFrameworkCore =
        new(() => BuildReferenceSet(excludeDiAbstractions: false, excludeEntityFrameworkCore: true));

    private static ImmutableArray<MetadataReference> ResolveReferences(
        bool excludeDiAbstractions,
        bool excludeEntityFrameworkCore) =>
        (excludeDiAbstractions, excludeEntityFrameworkCore) switch
        {
            (false, false) => AllReferences.Value,
            (true, false) => ReferencesWithoutDiAbstractions.Value,
            (true, true) => ReferencesWithoutDiAbstractionsAndEntityFrameworkCore.Value,
            (false, true) => ReferencesWithoutEntityFrameworkCore.Value,
        };

    private static ImmutableArray<MetadataReference> BuildReferenceSet(
        bool excludeDiAbstractions,
        bool excludeEntityFrameworkCore)
    {
        var trustedAssembliesString = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        var references = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (var assemblyPath in trustedAssembliesString.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(assemblyPath)) continue;
            if (excludeDiAbstractions && IsDiAbstractionsAssembly(assemblyPath)) continue;
            if (excludeEntityFrameworkCore && IsEntityFrameworkCoreAssembly(assemblyPath)) continue;
            references.Add(MetadataReference.CreateFromFile(assemblyPath));
        }
        references.Add(MetadataReference.CreateFromFile(typeof(GenerateFilterAttribute<>).Assembly.Location));
        return references.ToImmutable();
    }

    private static bool IsDiAbstractionsAssembly(string assemblyPath) =>
        string.Equals(
            Path.GetFileNameWithoutExtension(assemblyPath),
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsEntityFrameworkCoreAssembly(string assemblyPath) =>
        Path.GetFileNameWithoutExtension(assemblyPath)
            .StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase);
}
