using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests;

public class PipelineTests
{
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
