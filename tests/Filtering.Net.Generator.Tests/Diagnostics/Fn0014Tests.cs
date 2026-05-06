namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0014Tests
{
    [Fact]
    public void TwoProfilesForSameIntType_FiresFN0014()
    {
        // Arrange — Filtering.Net already ships [FilterProfile<int>] (Int32Filter); the
        // hand-written profile below makes int an ambiguous match on a [Map] without an
        // explicit Profile = typeof(...).
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
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0014");
    }

    [Fact]
    public void HandWrittenEnumProfileCollidesWithAutoEmitted_FiresFN0014()
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
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0014");
    }
}
