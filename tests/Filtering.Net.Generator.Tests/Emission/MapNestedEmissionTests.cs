namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Snapshot tests for <c>[MapNested]</c> emission across single-level, transitive, and knob-restricted scenarios.</summary>
public class MapNestedEmissionTests
{
    [Fact]
    public Task NestedSingleLevel_EmitsPrefixedDispatch()
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
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public Task NestedTwoLevel_EmitsFlatTransitivePaths()
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
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public Task NestedWithOnly_RestrictsDispatch()
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
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public Task NestedWithExcept_SkipsExcludedPath()
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
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public Task NestedWithCustomPrefix_EmitsAliasedDispatch()
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
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public Task NestedWithDisableSorting_OverridesSourceSortable()
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
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public Task NestedWithCustomProfile_InlinesOperatorBody()
    {
        // Arrange — DepartmentFilter maps Name through a custom profile that adds a `fuzzy` operator;
        // UserFilter splices the dept mappings, so the spliced Department.Name path must carry the
        // custom operator through.
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
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public Task NestedWithInterceptor_CallsThroughSourceWrapper()
    {
        // Arrange — interceptor on DepartmentFilter targets Email; we capture what the host UserFilter
        // emits for the spliced Department.Email path. The interceptor is carried by the lifted
        // FilterProperty and runs for the host's department.email path — see
        // MapNestedEndToEndRuntimeTests.Filter_SourceInterceptor_RunsThroughNestedLift.
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
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public Task NestedWithPropertyMap_LiftsOverrideUnderPrefix()
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
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public Task NestedSelfReferenceWithMaxDepth_EmitsBoundedNesting()
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
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

}
