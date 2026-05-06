namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0010Tests
{
    [Fact]
    public void FilterOperatorOnInstanceMember_FiresFN0010()
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
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0010");
    }

    [Fact]
    public void FilterOperatorOnPrivateStaticMember_FiresFN0010()
    {
        // Arrange — public is also required, not just static.
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
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0010");
    }

    [Fact]
    public void FilterOperatorOnPublicStaticMember_DoesNotFireFN0010()
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
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0010");
    }
}
