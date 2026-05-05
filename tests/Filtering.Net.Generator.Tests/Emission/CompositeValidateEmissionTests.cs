namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Snapshot tests for the composite Validate(FilterRequest) emission.</summary>
public class CompositeValidateEmissionTests
{
    [Fact]
    public async Task RequestValidator_AggregatesAllSubValidations()
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
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Sortable = true)]
                private static partial void MapName();

                [Map(nameof(User.Age), Sortable = true)]
                private static partial void MapAge();
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }
}
