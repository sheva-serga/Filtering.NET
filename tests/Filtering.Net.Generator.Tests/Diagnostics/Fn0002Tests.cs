using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0002 (DuplicateSortable): two [Map] entries for the same property both set Sortable=true.</summary>
public class Fn0002Tests
{
    [Fact]
    public void TwoSortableMapsForSameProperty_FiresFN0002()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Sortable = true)]
                private static partial void MapNameOne();
                [Map(nameof(User.Name), Sortable = true)]
                private static partial void MapNameTwo();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0002");
    }

    [Fact]
    public void DuplicateNonSortable_FiresFN0001NotFN0002()
    {
        // Arrange
        // When both duplicates are NOT sortable=true we want the general FN0001, never FN0002.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapNameOne();
                [Map(nameof(User.Name))]
                private static partial void MapNameTwo();
            }
            """;

        // Act
        var result = GeneratorRunner.RunDriver(source, excludeDiAbstractions: false).GetRunResult();

        // Assert
        var ids = result.Diagnostics.Select(diagnostic => diagnostic.Id).ToList();
        ids.Should().Contain("FN0001");
        ids.Should().NotContain("FN0002");
    }

    [Fact]
    public void SingleSortableMap_DoesNotFireFN0002()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Sortable = true)]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0002");
    }
}
