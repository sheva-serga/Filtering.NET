namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0013Tests
{
    [Fact]
    public void DuplicateOperatorNameOnProfile_FiresFN0013()
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

                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> EqAlt => (column, value) => column == value;
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0013");
    }

    [Fact]
    public void UniqueOperatorNamesOnProfile_DoNotFireFN0013()
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

                [FilterOperator("ne")]
                public static Expression<Func<string, string, bool>> Ne => (column, value) => column != value;
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0013");
    }

    [Fact]
    public void SameOperatorNameOnProfileAndBaseProfile_DoesNotFireFN0013()
    {
        // Arrange — per-profile check; an inheriting profile re-declaring an operator
        // present on its BasedOn target is intentional override, not a duplicate.
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class DerivedProfile
            {
                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> EqOverride => (column, value) => column == value;
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0013");
    }

    [Fact]
    public void OperatorNamesDifferingOnlyInCase_FireFN0013()
    {
        // Arrange — FilterProfile<TColumn> keys its operators case-insensitively, so these two are
        // one operator at runtime and the second throws from the generated static initialiser.
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class CustomProfile
            {
                [FilterOperator("isEmpty")]
                public static Expression<Func<string, bool>> IsEmpty => column => column.Length == 0;

                [FilterOperator("ISEMPTY")]
                public static Expression<Func<string, bool>> IsEmptyUpper => column => column.Length == 0;
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0013");
    }

    [Fact]
    public void DuplicateAcrossPropertyAndMethod_FiresFN0013()
    {
        // Arrange — [FilterOperator] is allowed on both Property and Method targets;
        // duplicate detection must work across both forms.
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

                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> EqMethod() => (column, value) => column == value;
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0013");
    }

    [Fact]
    public void DuplicateOperator_ReportsFirstOperatorAsAdditionalLocation()
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
                public static Expression<Func<string, string, bool>> EqFirst => (column, value) => column == value;

                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> EqSecond => (column, value) => column == value;
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0013", expectedAdditionalCount: 1);
    }
}
