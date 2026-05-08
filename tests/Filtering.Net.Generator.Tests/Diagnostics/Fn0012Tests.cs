namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0012Tests
{
    [Fact]
    public void InterceptValueWithoutMap_FiresFN0012()
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
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0012");
    }

    [Fact]
    public void InterceptValueWithMatchingMap_DoesNotFireFN0012()
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
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0012");
    }
}
