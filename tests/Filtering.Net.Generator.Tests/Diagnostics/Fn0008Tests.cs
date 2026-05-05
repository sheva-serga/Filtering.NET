namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0008 (MissingPartial): [Map] method is not declared partial.</summary>
public class Fn0008Tests
{
    [Fact]
    public void MapMethodWithoutPartial_FiresFN0008()
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
                private static void MapName() { }
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0008");
    }

    [Fact]
    public void MapMethodWithPartial_DoesNotFireFN0008()
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
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0008");
    }
}
