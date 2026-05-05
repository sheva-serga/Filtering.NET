namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN1004 (OperatorUnused): operator on a used profile is never named in any Only= list.</summary>
public class Fn1004Tests
{
    [Fact]
    public void OperatorExcludedByEveryConsumer_FiresFN1004()
    {
        // Arrange
        // Only one mapping uses the profile and it limits to "eq" — "ne" is therefore unused.
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
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Profile = typeof(CustomProfile), Only = new[] { "eq" })]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN1004");
    }

    [Fact]
    public void OperatorImplicitlyAllowedSomewhere_DoesNotFireFN1004()
    {
        // Arrange
        // The second mapping doesn't use Only=, so all operators are implicitly allowed.
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
            public class User { public string Name { get; set; } = ""; public string Other { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Profile = typeof(CustomProfile), Only = new[] { "eq" })]
                private static partial void MapName();
                [Map(nameof(User.Other), Profile = typeof(CustomProfile))]
                private static partial void MapOther();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1004");
    }
}
