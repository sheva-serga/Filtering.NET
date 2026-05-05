namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN1003 (ProfileUnused): a [FilterProfile&lt;T&gt;] is declared but no [Map(... Profile = typeof(X))] cites it.</summary>
public class Fn1003Tests
{
    [Fact]
    public void ProfileNeverReferenced_FiresFN1003()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class UnusedProfile
            {
                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> Eq => (column, value) => column == value;
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN1003");
    }

    [Fact]
    public void ProfileReferencedByMap_DoesNotFireFN1003()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class UsedProfile
            {
                [FilterOperator("eq")]
                public static Expression<Func<string, string, bool>> Eq => (column, value) => column == value;
            }
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Profile = typeof(UsedProfile))]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1003");
    }
}
