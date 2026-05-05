namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Snapshot tests for the Validate(int?, int?) emission — page bounds checks.</summary>
public class ValidatePageEmissionTests
{
    [Fact]
    public async Task PageSettingsApplied_EmitsBoundsValidator()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [PageSettings(MaxPageSize = 100, DefaultPageSize = 25)]
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
}
