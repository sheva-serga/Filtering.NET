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

        var models = new List<FilterClassModel>();
        foreach (var generatorResult in runResult.Results)
        {
            if (!generatorResult.TrackedSteps.TryGetValue(TrackingNames.FilterClassModels, out var steps))
                continue;
            foreach (var step in steps)
            {
                foreach (var (value, _) in step.Outputs)
                {
                    if (value is FilterClassModelWithDiagnostics { Model: { } model })
                        models.Add(model);
                }
            }
        }
        return models;
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

    private static CSharpCompilation BuildCompilation(
        string sourceCode,
        bool excludeDiAbstractions,
        bool excludeEntityFrameworkCore)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = ResolveReferences(excludeDiAbstractions, excludeEntityFrameworkCore);
        references.Add(MetadataReference.CreateFromFile(typeof(GenerateFilterAttribute<>).Assembly.Location));

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static List<MetadataReference> ResolveReferences(
        bool excludeDiAbstractions,
        bool excludeEntityFrameworkCore)
    {
        var trustedAssembliesString = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        var references = new List<MetadataReference>();
        foreach (var assemblyPath in trustedAssembliesString.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(assemblyPath)) continue;
            if (excludeDiAbstractions && IsDiAbstractionsAssembly(assemblyPath)) continue;
            if (excludeEntityFrameworkCore && IsEntityFrameworkCoreAssembly(assemblyPath)) continue;
            references.Add(MetadataReference.CreateFromFile(assemblyPath));
        }
        return references;
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
