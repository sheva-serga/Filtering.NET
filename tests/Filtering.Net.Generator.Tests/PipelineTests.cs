using AwesomeAssertions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Filtering.Net.Generator.Tests;

public class PipelineTests
{
    private const string SimpleFilterSource = """
        using Filtering.Net;
        namespace TestNs;
        public class User { public string Name { get; set; } = ""; }
        [GenerateFilter<User>]
        [Map(nameof(User.Name))]
        public partial class UserFilter
        {
        }
        """;

    [Fact]
    public void RunDriver_SecondRunOverAnEquivalentCompilation_ReusesEveryTrackedStep()
    {
        // Arrange — a cloned compilation is a new Compilation instance, so every node combined with
        // CompilationProvider re-executes; the value-equatable index keeps the outputs unchanged.
        var compilation = GeneratorRunner.BuildCompilation(SimpleFilterSource, excludeDiAbstractions: false);
        var driverOptions = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);
        var driver = CSharpGeneratorDriver.Create(
            generators: [new FilterGenerator().AsSourceGenerator()],
            additionalTexts: null,
            parseOptions: null,
            optionsProvider: null,
            driverOptions: driverOptions)
            .RunGenerators(compilation, TestContext.Current.CancellationToken);

        // Act
        var secondRunResult = driver
            .RunGenerators(compilation.Clone(), TestContext.Current.CancellationToken)
            .GetRunResult();

        // Assert
        var trackedSteps = secondRunResult.Results[0].TrackedSteps;
        string[] pipelineStepNames =
        [
            TrackingNames.GeneratorIndex,
            TrackingNames.FilterClassDeclarations,
            TrackingNames.FilterClassModels,
            TrackingNames.ResolvedFilterClassModels,
        ];
        foreach (var stepName in pipelineStepNames)
        {
            trackedSteps.Should().ContainKey(stepName);
            trackedSteps[stepName]
                .SelectMany(step => step.Outputs)
                .Should()
                .AllSatisfy(output => output.Reason.Should().BeOneOf(
                    IncrementalStepRunReason.Cached,
                    IncrementalStepRunReason.Unchanged),
                    because: $"step {stepName} must not produce a new value for an unchanged compilation");
        }
    }

    [Fact]
    public void RunDriver_AssemblyDefaultsChangedInAnotherFile_RefreshesEmittedPageSettings()
    {
        // Arrange — the filter class's own tree is untouched, so its syntax transform is served from
        // cache; the page settings must still come from the changed assembly attribute.
        const string AssemblyInfoTemplate = "[assembly: Filtering.Net.FilterDefaults(MaxPageSize = MAX_PAGE_SIZE)]";
        var firstCompilation = GeneratorRunner.BuildCompilation(
            [SimpleFilterSource, AssemblyInfoTemplate.Replace("MAX_PAGE_SIZE", "200")],
            excludeDiAbstractions: false);
        var secondCompilation = firstCompilation.ReplaceSyntaxTree(
            firstCompilation.SyntaxTrees[1],
            CSharpSyntaxTree.ParseText(
                AssemblyInfoTemplate.Replace("MAX_PAGE_SIZE", "500"),
                cancellationToken: TestContext.Current.CancellationToken));
        var driver = CSharpGeneratorDriver.Create(new FilterGenerator())
            .RunGenerators(firstCompilation, TestContext.Current.CancellationToken);

        // Act
        var secondRunResult = driver
            .RunGenerators(secondCompilation, TestContext.Current.CancellationToken)
            .GetRunResult();

        // Assert
        var emittedFilterSource = secondRunResult.GeneratedTrees
            .Single(tree => tree.FilePath.EndsWith("TestNs.UserFilter.g.cs", StringComparison.Ordinal))
            .ToString();
        emittedFilterSource.Should().Contain("FilterSettings(50, 500");
    }

    [Fact]
    public void RunDriver_NoFilterAttributes_ProducesNoOutput()
    {
        // Arrange
        var consumerSource = "namespace TestNs { public class Foo { } }";

        // Act
        var generatorRunResult = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false).GetRunResult();

        // Assert
        generatorRunResult.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void RunDriver_GenerateFilterOnNonPartialClass_IsIgnored()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs {
                public class User { public string Name { get; set; } = ""; }
                [GenerateFilter<User>]
                public class UserFilter { }   // not partial - should be ignored
            }
            """;

        // Act
        var generatorRunResult = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false).GetRunResult();

        // Assert
        generatorRunResult.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void RunDriver_GenerateFilterOnPartialClass_EmitsPartialClassFile()
    {
        // Arrange
        // The predicate-positive case actually emits a generated file. We just
        // assert that one file is produced and the hint name matches the convention; structural
        // assertions on the emitted body live in the snapshot tests.
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs {
                public class User { public string Name { get; set; } = ""; }
                [GenerateFilter<User>]
                public partial class UserFilter { }
            }
            """;

        // Act
        // The generator emits one .g.cs per filter class. An additional
        // FilteringServiceCollectionExtensions.g.cs is emitted whenever
        // Microsoft.Extensions.DependencyInjection.Abstractions is referenced.
        var generatorRunResult = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false).GetRunResult();

        // Assert
        var fileNames = generatorRunResult.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToList();
        fileNames.Should().Contain("TestNs.UserFilter.g.cs");
    }
}
