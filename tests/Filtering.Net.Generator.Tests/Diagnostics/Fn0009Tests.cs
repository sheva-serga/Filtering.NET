namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0009Tests
{
    [Fact]
    public void TwoInterceptorsForSameProperty_FiresFN0009()
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
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0009");
    }

    [Fact]
    public void SingleInterceptor_DoesNotFireFN0009()
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
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0009");
    }
}
