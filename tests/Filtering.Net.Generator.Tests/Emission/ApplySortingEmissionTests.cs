namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Snapshot tests for the ApplySorting emission — 4-way switch per sortable
/// property, plus Skip/Take pagination.</summary>
public class ApplySortingEmissionTests
{
    [Fact]
    public async Task TwoSortableProperties_EmitsFourArmsEach()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User
            {
                public string Name { get; set; } = "";
                public int Age { get; set; }
            }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Sortable = true)]
            [Map(nameof(User.Age), Sortable = true)]
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
