namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN1005 (ZeroOperatorsAllowed): Only/Except resolves to an empty operator set.</summary>
public class Fn1005Tests
{
    [Fact]
    public void ExceptRemovesAllOperators_FiresFN1005()
    {
        // Arrange
        // StringFilter operators: eq, ne, contains, startsWith, endsWith, in, isNull. Excluding
        // them all leaves nothing.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Except = new[] { "eq", "ne", "contains", "startsWith", "endsWith", "in", "isNull" })]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN1005");
    }

    [Fact]
    public void OnlyKeepsAtLeastOneOperator_DoesNotFireFN1005()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Only = new[] { "eq" })]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1005");
    }
}
