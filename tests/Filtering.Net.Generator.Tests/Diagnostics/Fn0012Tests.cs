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
    public void AmbiguousProfile_ReportsAllCandidateProfilesAsAdditionalLocations()
    {
        // Arrange — two hand-written profiles for the same enum so both candidate locations
        // resolve from source and the additional-location count is deterministic.
        var source = """
            using System;
            using System.Linq.Expressions;
            using System.Text.Json;
            using Filtering.Net;
            namespace TestNs;
            public enum Priority { Low, High }
            [FilterProfile<Priority>]
            public static class PriorityFilterA
            {
                [FilterOperator("eq")]
                public static Expression<Func<Priority, Priority, bool>> Eq => (column, value) => column == value;
                public static bool TryGetValue(JsonElement element, out Priority value, out string error)
                { value = Priority.Low; error = ""; return true; }
            }
            [FilterProfile<Priority>]
            public static class PriorityFilterB
            {
                [FilterOperator("eq")]
                public static Expression<Func<Priority, Priority, bool>> Eq => (column, value) => column == value;
                public static bool TryGetValue(JsonElement element, out Priority value, out string error)
                { value = Priority.Low; error = ""; return true; }
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
