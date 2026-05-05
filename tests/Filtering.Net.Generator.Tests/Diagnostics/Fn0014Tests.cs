namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0014 (InterceptorWithoutMap): [InterceptValue] for a property without a matching [Map].</summary>
public class Fn0014Tests
{
    [Fact]
    public void InterceptValueWithoutMap_FiresFN0014()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [InterceptValue(nameof(User.Name))]
                private static string TrimName(string value) => value.Trim();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0014");
    }

    [Fact]
    public void InterceptValueWithMatchingMap_DoesNotFireFN0014()
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
                [InterceptValue(nameof(User.Name))]
                private static string TrimName(string value) => value.Trim();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0014");
    }
}
