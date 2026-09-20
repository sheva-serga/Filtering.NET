namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0002 (MapAndPropertyMapBoth): a property declared on both [Map] and [PropertyMap].</summary>
public class Fn0002Tests
{
    [Fact]
    public void MapAndPropertyMapForSameProperty_FiresFN0002()
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
                [PropertyMap(nameof(User.Name))]
                private static void OverrideName(FilterRuleBuilder<User, string> rule) { }
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0002");
    }

    [Fact]
    public void MapOnly_DoesNotFireFN0002()
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
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0002");
    }

    [Fact]
    public void MapAndPropertyMapBoth_ReportsCollidingMapAsAdditionalLocation()
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
                [PropertyMap(nameof(User.Name))]
                private static void OverrideName(FilterRuleBuilder<User, string> rule) { }
            }
            """;

        // Act
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0002", expectedAdditionalCount: 1);
    }
}
