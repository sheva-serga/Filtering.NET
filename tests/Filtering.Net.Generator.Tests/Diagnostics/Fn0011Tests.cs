namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0011Tests
{
    [Fact]
    public void InterceptValueWithoutMap_FiresFN0011()
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
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0011");
    }

    [Fact]
    public void InterceptValueWithMatchingMap_DoesNotFireFN0011()
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
                private static string TrimName(string value) => value.Trim();
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0011");
    }
}
