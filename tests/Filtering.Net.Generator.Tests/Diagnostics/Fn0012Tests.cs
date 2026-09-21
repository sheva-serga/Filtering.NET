namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0012Tests
{
    [Fact]
    public void TwoProfilesForSameIntType_FiresFN0012()
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
            [Map(nameof(User.Id))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0012");
    }

    [Fact]
    public void HandWrittenEnumProfileCollidesWithAutoEmitted_FiresFN0012()
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
            [Map(nameof(User.Status))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0012");
    }

    [Fact]
    public void SingleBuiltInProfileForIntType_DoesNotFireFN0012()
    {
        // Arrange — only Int32Filter matches int, so resolution must settle on it silently. A
        // double-registration in the profile index would make every plain [Map] on an int an error.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public int Id { get; set; } }
            [GenerateFilter<User>]
            [Map(nameof(User.Id), Sortable = true)]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0012");
    }

    [Fact]
    public void OnlyTheAutoEmittedEnumProfileForEnumType_DoesNotFireFN0012()
    {
        // Arrange — the generator emits one profile per enum; if that emitted profile were also
        // registered as a source declaration, every enum [Map] would report an ambiguity.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public enum UserStatus { Active, Closed }
            public class User { public UserStatus Status { get; set; } }
            [GenerateFilter<User>]
            [Map(nameof(User.Status))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0012");
    }

    [Fact]
    public void AmbiguityResolvedByExplicitProfile_DoesNotFireFN0012()
    {
        // Arrange — same two-candidate shape as the first test, with Profile = typeof(...) naming one.
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
            [Map(nameof(User.Id), Profile = typeof(MyIntFilter), Sortable = true)]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0012");
    }

    [Fact]
    public void AmbiguousProfile_ReportsAllCandidateProfilesAsAdditionalLocations()
    {
        // Arrange — two hand-written profiles for the same enum so both candidate locations
        // resolve from source and the additional-location count is deterministic.
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            public enum Priority { Low, High }
            [FilterProfile<Priority>]
            public static class PriorityFilterA
            {
                [FilterOperator("eq")]
                public static Expression<Func<Priority, Priority, bool>> Eq => (column, value) => column == value;
            }
            [FilterProfile<Priority>]
            public static class PriorityFilterB
            {
                [FilterOperator("eq")]
                public static Expression<Func<Priority, Priority, bool>> Eq => (column, value) => column == value;
            }
            public class Ticket { public Priority Priority { get; set; } }
            [GenerateFilter<Ticket>]
            [Map(nameof(Ticket.Priority))]
            public partial class TicketFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0012", expectedAdditionalCount: 2);
    }
}
