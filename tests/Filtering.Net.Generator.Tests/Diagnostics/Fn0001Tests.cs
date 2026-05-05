namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0001 (DuplicateMap): two [Map] declarations target the same property.</summary>
public class Fn0001Tests
{
    [Fact]
    public void TwoMapsForSameProperty_FiresFN0001()
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
                private static partial void MapNameFirst();
                [Map(nameof(User.Name))]
                private static partial void MapNameSecond();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0001");
    }

    [Fact]
    public void DistinctProperties_DoesNotFireFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public int Age { get; set; } }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
                [Map(nameof(User.Age))]
                private static partial void MapAge();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0001");
    }
}
