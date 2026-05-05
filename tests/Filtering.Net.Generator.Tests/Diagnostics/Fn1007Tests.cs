namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN1007 (UntranslatableMethodInOperator): operator body uses a method outside the EF Core allow-list.</summary>
public class Fn1007Tests
{
    [Fact]
    public void OperatorBodyUsesUnknownMethod_FiresFN1007()
    {
        // Arrange
        // FormatNicely is a user method — not in the allow-list — so it should trip FN1007.
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class CustomProfile
            {
                public static string FormatNicely(string value) => value.Trim();

                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> Eq =>
                    (column, value) => column == FormatNicely(value);
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN1007");
    }

    [Fact]
    public void OperatorBodyUsesAllowListedContains_DoesNotFireFN1007()
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
                [FilterOperator("contains")]
                public static Expression<Func<string, string, bool>> Contains =>
                    (column, value) => column.Contains(value);
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1007");
    }

    /// <summary>
    /// When the consumer references EF Core, any <c>EF.Functions.*</c> method should be
    /// considered translatable — even invented npgsql-style extensions we've never heard of.
    /// </summary>
    [Fact]
    public void OperatorBodyUsesEfFunctionsCustomMethod_DoesNotFireFN1007_WhenEfReferenced()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            using Microsoft.EntityFrameworkCore;
            namespace TestNs;
            [FilterProfile<string>]
            public static class CustomProfile
            {
                // TrigramsAreSimilar is an npgsql extension we deliberately don't pre-seed in the
                // allow-list. With EF Core in the project graph it should be accepted.
                [FilterOperator("similar")]
                public static Expression<Func<string, string, bool>> Similar =>
                    (column, value) => EF.Functions.Like(column, value + "%");
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1007");
    }

    /// <summary>
    /// Without EF Core in the project graph an unknown <c>EF.Functions.*</c> method is still
    /// flagged by FN1007 — the analyzer has no way to know what's translatable in that scenario.
    /// </summary>
    [Fact]
    public void OperatorBodyUsesEfFunctionsUnknownMethod_FiresFN1007_WhenEfNotReferenced()
    {
        // Arrange
        // Use a name that's NOT pre-seeded in EfTranslatableMethods (Like/ILike/Collate are seeded).
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            // Stub the EF.Functions surface so the source compiles without referencing EF Core.
            public static class EF
            {
                public static class Functions
                {
                    public static bool TrigramsAreSimilar(string left, string right) => false;
                }
            }
            [FilterProfile<string>]
            public static class CustomProfile
            {
                [FilterOperator("similar")]
                public static Expression<Func<string, string, bool>> Similar =>
                    (column, value) => EF.Functions.TrigramsAreSimilar(column, value);
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN1007", excludeEntityFrameworkCore: true);
    }
}
