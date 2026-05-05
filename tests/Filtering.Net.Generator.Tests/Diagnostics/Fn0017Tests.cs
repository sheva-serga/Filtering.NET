namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0017 (DuplicateOperatorOnProfile): the same operator name must not
/// appear on more than one [FilterOperator]-marked member of the same profile.</summary>
public class Fn0017Tests
{
    [Fact]
    public void DuplicateOperatorNameOnProfile_FiresFN0017()
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

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0017");
    }

    [Fact]
    public void UniqueOperatorNamesOnProfile_DoNotFireFN0017()
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

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0017");
    }

    [Fact]
    public void SameOperatorNameOnProfileAndBaseProfile_DoesNotFireFN0017()
    {
        // Arrange
        // The check is per-profile. An inheriting profile that re-declares an operator
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

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0017");
    }

    [Fact]
    public void DuplicateAcrossPropertyAndMethod_FiresFN0017()
    {
        // Arrange
        // [FilterOperator] is allowed on both Property and Method targets.
        // Duplicate detection must work across both forms.
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

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0017");
    }
}
