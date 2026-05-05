namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0003 (MapAndPropertyMapBoth): a property declared on both [Map] and [PropertyMap].</summary>
public class Fn0003Tests
{
    [Fact]
    public void MapAndPropertyMapForSameProperty_FiresFN0003()
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
                [PropertyMap(nameof(User.Name))]
                private static void OverrideName(FilterRuleBuilder<User, string> rule) { }
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0003");
    }

    [Fact]
    public void MapOnly_DoesNotFireFN0003()
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
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0003");
    }
}
