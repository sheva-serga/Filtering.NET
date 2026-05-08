namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Asserts each <see cref="MapNestedEmissionTests"/> scenario's emitted output compiles cleanly. Catches regressions where the snapshot matches but the C# is malformed (duplicate switch cases, broken lambdas).</summary>
public class MapNestedCompilesTests
{
    [Fact]
    public void NestedSingleLevel_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public string Name { get; set; } = ""; public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [Map(nameof(Department.Name))] private static partial void MapName();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))] private static partial void MapName();
                [MapNested(nameof(User.Department))] private static partial void MapDept();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void NestedTwoLevel_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class Company { public string Name { get; set; } = ""; }
            public class Department { public Company Company { get; set; } = new(); }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Company>] public partial class CompanyFilter
            {
                [Map(nameof(Company.Name))] private static partial void MapName();
            }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [MapNested(nameof(Department.Company))] private static partial void MapCompany();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department))] private static partial void MapDept();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void NestedWithOnly_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public int Id { get; set; } public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [Map(nameof(Department.Id))] private static partial void MapId();
                [Map(nameof(Department.Name))] private static partial void MapName();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department), Only = new[] { "Id" })]
                private static partial void MapDept();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void NestedWithExcept_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public int Id { get; set; } public string Name { get; set; } = ""; public string InternalNotes { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [Map(nameof(Department.Id))] private static partial void MapId();
                [Map(nameof(Department.Name))] private static partial void MapName();
                [Map(nameof(Department.InternalNotes))] private static partial void MapNotes();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department), Except = new[] { "InternalNotes" })]
                private static partial void MapDept();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void NestedWithCustomPrefix_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public int Id { get; set; } }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [Map(nameof(Department.Id))] private static partial void MapId();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department), Prefix = "dept")]
                private static partial void MapDept();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void NestedWithDisableSorting_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [Map(nameof(Department.Name), Sortable = true)]
                private static partial void MapName();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department), DisableSorting = true)]
                private static partial void MapDept();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void NestedWithCustomProfile_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            using System.Linq.Expressions;
            namespace TestNs;
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class StringFilterPlus
            {
                [FilterOperator("fuzzy")]
                public static Expression<System.Func<string, string, bool>> Fuzzy =>
                    (column, value) => column.Contains(value);
            }
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [Map(nameof(Department.Name), Profile = typeof(StringFilterPlus))]
                private static partial void MapName();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department))] private static partial void MapDept();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void NestedWithInterceptor_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Email { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [Map(nameof(Department.Email))] private static partial void MapEmail();
                [InterceptValue(nameof(Department.Email))]
                internal static string LowercaseEmail(InterceptContext ctx, string value) => value.ToLowerInvariant();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department))] private static partial void MapDept();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void NestedWithNullableNav_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department? Department { get; set; } }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [Map(nameof(Department.Name))] private static partial void MapName();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department))] private static partial void MapDept();
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }
}
