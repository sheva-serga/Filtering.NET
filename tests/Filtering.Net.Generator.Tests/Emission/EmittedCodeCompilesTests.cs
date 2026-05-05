namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Sanity checks that the generated C# is well-formed and compiles. These run on
/// every generator sub-feature so syntax errors are caught immediately rather than via a snapshot
/// diff later.</summary>
public class EmittedCodeCompilesTests
{
    [Fact]
    public void Skeleton_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void NumericFilter_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public int Age { get; set; } }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Age))]
                private static partial void MapAge();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void CustomProfileWithCustomOperator_Compiles()
    {
        // Arrange
        var consumerSource = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace Sample;
            public class User { public string Name { get; set; } = ""; }
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class FuzzyStringFilter
            {
                [FilterOperator("fuzzy")]
                public static Expression<Func<string, string, bool>> Fuzzy => (column, value) => column.Contains(value);
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Profile = typeof(FuzzyStringFilter))]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void CustomProfileWithDateTimeOperator_Compiles()
    {
        // Arrange
        var consumerSource = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace Sample;
            public class Audit { public DateTime CreatedAt { get; set; } }
            [FilterProfile<DateTime>(BasedOn = typeof(DateTimeFilter))]
            public static class RecencyFilter
            {
                [FilterOperator("withinDays")]
                public static Expression<Func<DateTime, int, bool>> WithinDays
                    => (date, days) => date >= DateTime.UtcNow.AddDays(-days);
            }
            [GenerateFilter<Audit>]
            public partial class AuditFilter
            {
                [Map(nameof(Audit.CreatedAt), Profile = typeof(RecencyFilter), Sortable = true)]
                private static partial void MapCreatedAt();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void MultipleProperties_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User
            {
                public string Name { get; set; } = "";
                public int Age { get; set; }
                public bool Active { get; set; }
                public System.Guid Id { get; set; }
                public System.DateTime CreatedAt { get; set; }
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();

                [Map(nameof(User.Age), Sortable = true)]
                private static partial void MapAge();

                [Map(nameof(User.Active))]
                private static partial void MapActive();

                [Map(nameof(User.Id))]
                private static partial void MapId();

                [Map(nameof(User.CreatedAt), Sortable = true)]
                private static partial void MapCreatedAt();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }
}
