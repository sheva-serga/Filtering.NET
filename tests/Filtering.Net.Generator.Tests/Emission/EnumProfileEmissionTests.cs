namespace Filtering.Net.Generator.Tests.Emission;

public class EnumProfileEmissionTests
{
    [Fact]
    public Task SingleEnum_EmitsAllOperators()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;

            public enum UserStatus { Active, Closed }
            public class User { public UserStatus Status { get; set; } }

            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Status))]
                private static partial void MapStatus();
            }
            """;
        var generatorRunResult = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false).GetRunResult();
        var enumProfileSource = generatorRunResult.GeneratedTrees
            .Single(tree => tree.FilePath.EndsWith("UserStatusFilter.g.cs", StringComparison.Ordinal))
            .ToString();

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(enumProfileSource).UseDirectory("Snapshots");
    }
}
