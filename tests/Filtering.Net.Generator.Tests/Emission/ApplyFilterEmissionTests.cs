namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Snapshot tests for the ApplyFilter emission — typed leaf builders,
/// PredicateBuilder composition, navigation accessors.</summary>
public class ApplyFilterEmissionTests
{
    [Fact]
    public async Task StringFilter_EmitsTypedLeaves()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task NumericFilter_EmitsCompareLeaves()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public int Age { get; set; } }
            [GenerateFilter<User>]
            [Map(nameof(User.Age))]
            public partial class UserFilter
            {
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task NavigationPath_EmitsDottedAccessor()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<User>]
            [Map("Department.Name", Alias = "dept")]
            public partial class UserFilter
            {
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }
}
