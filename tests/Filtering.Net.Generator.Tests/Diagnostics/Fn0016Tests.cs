namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0016Tests
{
    [Fact]
    public void DuplicateOperatorNameOnProfile_FiresFN0016()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using System.Text.Json;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class CustomProfile
            {
                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> Eq => (column, value) => column == value;

                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> EqAlt => (column, value) => column == value;

                public static bool TryGetValue(JsonElement element, out string value, out string error)
                {
                    value = ""; error = ""; return true;
                }
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0016");
    }

    [Fact]
    public void UniqueOperatorNamesOnProfile_DoNotFireFN0016()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using System.Text.Json;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class CustomProfile
            {
                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> Eq => (column, value) => column == value;

                [FilterOperator("ne")]
                public static Expression<Func<string, string, bool>> Ne => (column, value) => column != value;

                public static bool TryGetValue(JsonElement element, out string value, out string error)
                {
                    value = ""; error = ""; return true;
                }
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0016");
    }

    [Fact]
    public void SameOperatorNameOnProfileAndBaseProfile_DoesNotFireFN0016()
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
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0016");
    }

    [Fact]
    public void DuplicateAcrossPropertyAndMethod_FiresFN0016()
    {
        // Arrange — [FilterOperator] is allowed on both Property and Method targets;
        // duplicate detection must work across both forms.
        var source = """
            using System;
            using System.Linq.Expressions;
            using System.Text.Json;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class CustomProfile
            {
                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> Eq => (column, value) => column == value;

                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> EqMethod() => (column, value) => column == value;

                public static bool TryGetValue(JsonElement element, out string value, out string error)
                {
                    value = ""; error = ""; return true;
                }
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0016");
    }
}
