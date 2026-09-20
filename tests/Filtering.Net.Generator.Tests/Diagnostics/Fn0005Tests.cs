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
            [Map(nameof(User.Name), Only = new[] { "eq", "iAmNotARealOp" })]
            public partial class UserFilter
            {
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
            [Map(nameof(User.Name), Except = new[] { "iAmNotARealOp" })]
            public partial class UserFilter
            {
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
            [Map(nameof(User.Name), Only = new[] { "eq", "ne" })]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0005");
    }
}
