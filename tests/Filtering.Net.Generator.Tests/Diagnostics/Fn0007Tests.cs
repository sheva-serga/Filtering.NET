namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0007Tests
{
    [Fact]
    public void TwoInterceptorsForSameProperty_FiresFN0007()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
                [InterceptValue(nameof(User.Name))]
                private static string InterceptOne(string value) => value;
                [InterceptValue(nameof(User.Name))]
                private static string InterceptTwo(string value) => value;
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0007");
    }

    [Fact]
    public void SingleInterceptor_DoesNotFireFN0007()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
                [InterceptValue(nameof(User.Name))]
                private static string InterceptOne(string value) => value;
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0007");
    }

    [Fact]
    public void DuplicateInterceptor_ReportsFirstInterceptorAsAdditionalLocation()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
                [InterceptValue(nameof(User.Name))]
                private static string InterceptOne(string value) => value;
                [InterceptValue(nameof(User.Name))]
                private static string InterceptTwo(string value) => value;
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0007", expectedAdditionalCount: 1);
    }
}
