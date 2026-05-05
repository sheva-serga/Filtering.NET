namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN1001 (DateTimeUtcNowInLambda): operator body references DateTime.UtcNow / DateTimeOffset.Now etc.</summary>
public class Fn1001Tests
{
    [Fact]
    public void DateTimeUtcNowInOperatorBody_FiresFN1001()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<DateTime>]
            public static class CutoffProfile
            {
                [FilterOperator("withinDay")]
                public static Expression<Func<DateTime, bool>> WithinDay =>
                    column => column >= DateTime.UtcNow.AddDays(-1);
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN1001");
    }

    [Fact]
    public void DateTimeOffsetNowInOperatorBody_FiresFN1001()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<DateTimeOffset>]
            public static class CutoffProfile
            {
                [FilterOperator("after")]
                public static Expression<Func<DateTimeOffset, bool>> After =>
                    column => column >= DateTimeOffset.Now;
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN1001");
    }

    [Fact]
    public void OperatorBodyWithoutClockReference_DoesNotFireFN1001()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<int>]
            public static class CleanProfile
            {
                [FilterOperator("eq")]
                public static Expression<Func<int, int, bool>> Eq => (column, value) => column == value;
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1001");
    }
}
