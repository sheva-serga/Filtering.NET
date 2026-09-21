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
            [Map(nameof(User.Name), Profile = typeof(UsedProfile))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1003");
    }

    private const string BaseOnlyReferencedProfileSource = """
        using System;
        using System.Linq.Expressions;
        using Filtering.Net;
        namespace TestNs;
        [FilterProfile<string>(BasedOn = typeof(StringFilter))]
        public static class BaseTextProfile
        {
            [FilterOperator("fuzzy")]
            public static Expression<Func<string, string, bool>> Fuzzy => (column, value) => column.Contains(value);
        }
        [FilterProfile<string>(BasedOn = typeof(BaseTextProfile))]
        public static class DerivedTextProfile
        {
            [FilterOperator("fuzzier")]
            public static Expression<Func<string, string, bool>> Fuzzier => (column, value) => column.Contains(value);
        }
        public class User { public string Name { get; set; } = ""; }
        [GenerateFilter<User>]
        [Map(nameof(User.Name), Profile = typeof(DerivedTextProfile))]
        public partial class UserFilter
        {
        }
        """;

    [Fact]
    public void ProfileReferencedOnlyAsAnotherProfilesBasedOn_DoesNotFireFN1003()
    {
        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert — a bridge is emitted for BaseTextProfile and DerivedTextProfile inherits its
        // operators, so reporting it as unused would break the build under TreatWarningsAsErrors.
        DiagnosticTestHelpers.AssertNoDiagnostic(BaseOnlyReferencedProfileSource, "FN1003");
    }

    [Fact]
    public void OperatorInheritedThroughBasedOn_DoesNotFireFN1004()
    {
        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert — the leaf profile's AllowedOperators carry the inherited 'fuzzy', so the base
        // profile's declaration of it is reachable.
        DiagnosticTestHelpers.AssertNoDiagnostic(BaseOnlyReferencedProfileSource, "FN1004");
    }
}
