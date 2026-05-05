namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0005 (IncompatibleProfile): explicit Profile = typeof(...) doesn't match the property's CLR type.</summary>
public class Fn0005Tests
{
    [Fact]
    public void Int32FilterOnStringProperty_FiresFN0005()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Profile = typeof(Int32Filter))]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0005");
    }

    [Fact]
    public void StringFilterOnStringProperty_DoesNotFireFN0005()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Profile = typeof(StringFilter))]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0005");
    }
}
