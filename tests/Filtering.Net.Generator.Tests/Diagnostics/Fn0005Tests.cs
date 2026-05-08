namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0005 (UnknownOperator): Only/Except references an operator not on the resolved profile.</summary>
public class Fn0005Tests
{
    [Fact]
    public void OnlyReferencesUnknownOperator_FiresFN0005()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Only = new[] { "eq", "iAmNotARealOp" })]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0005");
    }

    [Fact]
    public void ExceptReferencesUnknownOperator_FiresFN0005()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Except = new[] { "iAmNotARealOp" })]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0005");
    }

    [Fact]
    public void OnlyReferencesValidOperators_DoesNotFireFN0005()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Only = new[] { "eq", "ne" })]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0005");
    }
}
