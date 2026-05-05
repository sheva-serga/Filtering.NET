namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0004 (PropertyNotFound): [Map] references a property that doesn't exist on the entity.</summary>
public class Fn0004Tests
{
    [Fact]
    public void PropertyNameNotOnEntity_FiresFN0004()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map("DoesNotExist")]
                private static partial void MapBad();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0004");
    }

    [Fact]
    public void PropertyExists_DoesNotFireFN0004()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0004");
    }
}
