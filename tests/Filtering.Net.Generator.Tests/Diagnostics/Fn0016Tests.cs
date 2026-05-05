namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0016 (ProfileMissingExtractor): a standalone <c>[FilterProfile&lt;T&gt;]</c>
/// must declare <c>TryGetValue</c> (and <c>TryGetArray</c> when the <c>in</c> operator is configured)
/// when no <c>BasedOn</c> is set.</summary>
public class Fn0016Tests
{
    [Fact]
    public void StandaloneProfileWithScalarOperator_AndNoTryGetValue_FiresFN0016()
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
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0016");
    }

    [Fact]
    public void StandaloneProfileWithInOperator_AndNoTryGetArray_FiresFN0016()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq;
            using System.Linq.Expressions;
            using System.Text.Json;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class CustomProfile
            {
                [FilterOperator("in")]
                public static Expression<Func<string, string[], bool>> In => (column, values) => values.Contains(column);

                public static bool TryGetValue(JsonElement element, out string value, out string error)
                {
                    value = ""; error = ""; return true;
                }
                // Missing TryGetArray — should fire FN0016.
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0016");
    }

    [Fact]
    public void StandaloneProfileWithBothExtractors_DoesNotFireFN0016()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq;
            using System.Linq.Expressions;
            using System.Text.Json;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class CustomProfile
            {
                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> Eq => (column, value) => column == value;

                [FilterOperator("in")]
                public static Expression<Func<string, string[], bool>> In => (column, values) => values.Contains(column);

                public static bool TryGetValue(JsonElement element, out string value, out string error)
                {
                    value = ""; error = ""; return true;
                }

                public static bool TryGetArray(JsonElement element, out string[] values, out string error)
                {
                    values = []; error = ""; return true;
                }
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0016");
    }

    [Fact]
    public void ProfileWithBasedOn_DoesNotFireFN0016_EvenWithoutOwnExtractors()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class DerivedProfile
            {
                [FilterOperator("fuzzy")]
                public static Expression<Func<string, string, bool>> Fuzzy => (column, value) => column.Contains(value);
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0016");
    }

    [Fact]
    public void StandaloneProfileWithOnlyIsNullOperator_DoesNotFireFN0016()
    {
        // Arrange
        // isNull is a None-shape operator that uses neither TryGetValue nor TryGetArray.
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class IsNullOnlyProfile
            {
                [FilterOperator("isNull")]
                public static Expression<Func<string, bool>> IsNull => column => column == null;
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0016");
    }

    [Fact]
    public void StandaloneProfileWithNoOperators_DoesNotFireFN0016()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class EmptyProfile { }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0016");
    }

    [Fact]
    public void StandaloneProfileWithScalarAndIn_AndOnlyTryGetValue_FiresFN0016ForTryGetArray()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq;
            using System.Linq.Expressions;
            using System.Text.Json;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class CustomProfile
            {
                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> Eq => (column, value) => column == value;

                [FilterOperator("in")]
                public static Expression<Func<string, string[], bool>> In => (column, values) => values.Contains(column);

                public static bool TryGetValue(JsonElement element, out string value, out string error)
                {
                    value = ""; error = ""; return true;
                }
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0016");
    }

    [Fact]
    public void StandaloneProfileWithNonStaticTryGetValue_FiresFN0016()
    {
        // Arrange
        // The check requires public static — instance methods don't count.
        var source = """
            using System;
            using System.Linq.Expressions;
            using System.Text.Json;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public class CustomProfile
            {
                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> Eq => (column, value) => column == value;

                public bool TryGetValue(JsonElement element, out string value, out string error)
                {
                    value = ""; error = ""; return true;
                }
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0016");
    }
}
