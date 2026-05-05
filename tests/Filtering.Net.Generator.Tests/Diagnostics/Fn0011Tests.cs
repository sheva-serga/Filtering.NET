namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0011 (NonStaticOperator): [FilterOperator] member must be public static.</summary>
public class Fn0011Tests
{
    [Fact]
    public void FilterOperatorOnInstanceMember_FiresFN0011()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public class CustomProfile
            {
                [FilterOperator("eq")]
                public Expression<Func<string, string, bool>> Eq => (column, value) => column == value;
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0011");
    }

    [Fact]
    public void FilterOperatorOnPrivateStaticMember_FiresFN0011()
    {
        // Arrange
        // public is also required, not just static.
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class CustomProfile
            {
                [FilterOperator("eq")]
                private static Expression<Func<string, string, bool>> Eq => (column, value) => column == value;
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0011");
    }

    [Fact]
    public void FilterOperatorOnPublicStaticMember_DoesNotFireFN0011()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class CustomProfile
            {
                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> Eq => (column, value) => column == value;
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0011");
    }
}
