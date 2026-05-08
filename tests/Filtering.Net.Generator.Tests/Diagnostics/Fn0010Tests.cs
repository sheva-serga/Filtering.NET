namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0010Tests
{
    [Fact]
    public void AliasMatchesAnotherPropertyName_FiresFN0010()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public string Nickname { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
                [Map(nameof(User.Nickname), Alias = "name")]
                private static partial void MapNickname();
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0010");
    }

    [Fact]
    public void TwoAliasesIdentical_FiresFN0010()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public string Nickname { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Alias = "title")]
                private static partial void MapName();
                [Map(nameof(User.Nickname), Alias = "TITLE")]
                private static partial void MapNickname();
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0010");
    }

    [Fact]
    public void DistinctAliases_DoesNotFireFN0010()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public string Nickname { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Alias = "fullName")]
                private static partial void MapName();
                [Map(nameof(User.Nickname), Alias = "shortName")]
                private static partial void MapNickname();
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0010");
    }

    [Fact]
    public void AliasCollision_ReportsCollidingPropertyAsAdditionalLocation()
    {
        // Arrange — 2-site collision: Nickname's alias collides with Name's property name.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public string Nickname { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
                [Map(nameof(User.Nickname), Alias = "name")]
                private static partial void MapNickname();
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0010", expectedAdditionalCount: 1);
    }
}
