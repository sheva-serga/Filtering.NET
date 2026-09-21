namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Asserts every <see cref="MapNestedEmissionTests"/> scenario's emitted output compiles cleanly, plus the nullable-navigation shape whose snapshot was dropped because it could not be distinguished. Catches regressions where the snapshot matches but the emitted C# is malformed.</summary>
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
            [Map(nameof(Department.Name))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
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
            [Map(nameof(Company.Name))]
            [GenerateFilter<Company>] public partial class CompanyFilter
            {
            }
            [MapNested(nameof(Department.Company))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
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
            [Map(nameof(Department.Id))]
            [Map(nameof(Department.Name))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department), Only = new[] { "Id" })]
            public partial class UserFilter
            {
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
            [Map(nameof(Department.Id))]
            [Map(nameof(Department.Name))]
            [Map(nameof(Department.InternalNotes))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department), Except = new[] { "InternalNotes" })]
            public partial class UserFilter
            {
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
            [Map(nameof(Department.Id))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department), Prefix = "dept")]
            public partial class UserFilter
            {
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
            [Map(nameof(Department.Name), Sortable = true)]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department), DisableSorting = true)]
            public partial class UserFilter
            {
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
            [Map(nameof(Department.Name), Profile = typeof(StringFilterPlus))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
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
            [Map(nameof(Department.Email))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [InterceptValue(nameof(Department.Email))]
                internal static string LowercaseEmail(InterceptContext ctx, string value) => value.ToLowerInvariant();
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void NestedWithPropertyMap_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            using System.Collections.Generic;
            using System.Linq;
            namespace TestNs;
            public class Department { public List<string> Tags { get; set; } = new(); }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [PropertyMap(nameof(Department.Tags))]
                private static FilterRule<Department, string> MapTags(FilterRuleBuilder<Department, string> builder) =>
                    builder.For(d => d.Tags.FirstOrDefault() ?? "")
                        .Operator("anyEq", (string tags, string v) => tags == v);
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void NestedSelfReferenceWithMaxDepth_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace TestNs;
            public class Employee { public string Name { get; set; } = ""; public Employee? Manager { get; set; } }
            [GenerateFilter<Employee>]
            [Map(nameof(Employee.Name))]
            [MapNested(nameof(Employee.Manager), MaxDepth = 2)]
            public partial class EmployeeFilter
            {
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
            [Map(nameof(Department.Name))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }
}
