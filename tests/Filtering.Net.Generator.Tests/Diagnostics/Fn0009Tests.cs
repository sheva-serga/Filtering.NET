namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0009Tests
{
    [Fact]
    public void AliasMatchesAnotherPropertyName_FiresFN0009()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public string Nickname { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [Map(nameof(User.Nickname), Alias = "name")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0009");
    }

    [Fact]
    public void TwoAliasesIdentical_FiresFN0009()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public string Nickname { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Alias = "title")]
            [Map(nameof(User.Nickname), Alias = "TITLE")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0009");
    }

    [Fact]
    public void DistinctAliases_DoesNotFireFN0009()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public string Nickname { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Alias = "fullName")]
            [Map(nameof(User.Nickname), Alias = "shortName")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0009");
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
            [Map(nameof(User.Name))]
            [Map(nameof(User.Nickname), Alias = "name")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0009", expectedAdditionalCount: 1);
    }
}
