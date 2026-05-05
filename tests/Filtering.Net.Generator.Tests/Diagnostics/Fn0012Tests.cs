namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0012 (AliasCollision): an alias collides (case-insensitively) with a property name or another alias.</summary>
public class Fn0012Tests
{
    [Fact]
    public void AliasMatchesAnotherPropertyName_FiresFN0012()
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
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0012");
    }

    [Fact]
    public void TwoAliasesIdentical_FiresFN0012()
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
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0012");
    }

    [Fact]
    public void DistinctAliases_DoesNotFireFN0012()
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
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0012");
    }
}
