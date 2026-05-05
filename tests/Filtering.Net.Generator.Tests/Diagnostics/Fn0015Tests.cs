namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0015 (AmbiguousProfile): two or more [FilterProfile&lt;T&gt;]
/// classes target the same CLR type and the property has no [Map(... Profile = typeof(X))] to disambiguate.</summary>
public class Fn0015Tests
{
    [Fact]
    public void TwoProfilesForSameIntType_FiresFN0015()
    {
        // Arrange
        // The harness compilation references Filtering.Net, which already ships a
        // [FilterProfile<int>] (Int32Filter). Declaring a second [FilterProfile<int>]
        // here puts two candidates in the ProfileIndex for property Id (int),
        // and the [Map(nameof(User.Id))] has no Profile = typeof(...) override —
        // so the index path must emit FN0015.
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<int>]
            public static class MyIntFilter
            {
                [FilterOperator("eq")]
                public static Expression<Func<int, int, bool>> Eq => (column, value) => column == value;
            }
            public class User { public int Id { get; set; } }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Id))]
                private static partial void MapId();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0015");
    }

    [Fact]
    public void HandWrittenEnumProfileCollidesWithAutoEmitted_FiresFN0015()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;

            namespace TestNs;

            public enum UserStatus { Active, Closed }

            [FilterProfile<UserStatus>]
            public static class MyUserStatusFilter
            {
                [FilterOperator("eq")]
                public static Expression<Func<UserStatus, UserStatus, bool>> Eq => (column, value) => column == value;
            }

            public class User { public UserStatus Status { get; set; } }

            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Status))]
                private static partial void MapStatus();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0015");
    }
}
