namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0010 (DuplicateInterceptor): two [InterceptValue] declarations target the same property.</summary>
public class Fn0010Tests
{
    [Fact]
    public void TwoInterceptorsForSameProperty_FiresFN0010()
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
                private static string InterceptOne(string value) => value;
                [InterceptValue(nameof(User.Name))]
                private static string InterceptTwo(string value) => value;
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0010");
    }

    [Fact]
    public void SingleInterceptor_DoesNotFireFN0010()
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
                private static string InterceptOne(string value) => value;
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0010");
    }
}
