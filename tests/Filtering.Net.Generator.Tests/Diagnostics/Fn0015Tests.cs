namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0015Tests
{
    [Fact]
    public void StandaloneProfileWithScalarOperator_AndNoTryGetValue_FiresFN0015()
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
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0015");
    }

    [Fact]
    public void StandaloneProfileWithInOperator_AndNoTryGetArray_FiresFN0015()
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
                // Missing TryGetArray.
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0015");
    }

    [Fact]
    public void StandaloneProfileWithBothExtractors_DoesNotFireFN0015()
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
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0015");
    }

    [Fact]
    public void ProfileWithBasedOn_DoesNotFireFN0015_EvenWithoutOwnExtractors()
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
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0015");
    }

    [Fact]
    public void StandaloneProfileWithOnlyIsNullOperator_DoesNotFireFN0015()
    {
        // Arrange — isNull is None-shape and uses neither TryGetValue nor TryGetArray.
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
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0015");
    }

    [Fact]
    public void StandaloneProfileWithNoOperators_DoesNotFireFN0015()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class EmptyProfile { }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0015");
    }

    [Fact]
    public void StandaloneProfileWithScalarAndIn_AndOnlyTryGetValue_FiresFN0015ForTryGetArray()
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
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0015");
    }

    [Fact]
    public void StandaloneProfileWithNonStaticTryGetValue_FiresFN0015()
    {
        // Arrange — public static is the contract; instance methods don't count.
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
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0015");
    }
}
