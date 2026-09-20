using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests;

public class ExtractionTests
{
    [Fact]
    public void RunDriver_SingleStringMap_ExtractsModelWithoutDiagnostics()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var generatorRunResult = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false).GetRunResult();

        // Assert
        // Note: emission is separate from extraction, so no generated trees yet. We're verifying extraction happens cleanly.
        generatorRunResult.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void RunDriver_DuplicateMapForSameProperty_EmitsFN0001()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var generatorRunResult = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false).GetRunResult();

        // Assert
        generatorRunResult.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0001");
    }

    [Fact]
    public void RunDriver_PropertyNotFoundOnEntity_EmitsFN0003()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map("Nonexistent")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var generatorRunResult = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false).GetRunResult();

        // Assert
        generatorRunResult.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0003");
    }
}
