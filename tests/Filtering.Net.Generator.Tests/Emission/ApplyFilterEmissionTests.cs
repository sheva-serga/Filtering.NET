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
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
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
            public partial class UserFilter
            {
                [Map(nameof(User.Age))]
                private static partial void MapAge();
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
            public partial class UserFilter
            {
                [Map("Department.Name", Alias = "dept")]
                private static partial void MapDeptName();
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }
}
