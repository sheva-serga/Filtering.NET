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
        // emits for the spliced Department.Email path. Interceptor splice-through is a vNext concern;
        // this snapshot pins the current behaviour either way.
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
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department))] private static partial void MapDept();
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
            public partial class EmployeeFilter
            {
                [Map(nameof(Employee.Name))] private static partial void MapName();
                [MapNested(nameof(Employee.Manager), MaxDepth = 2)] private static partial void MapManager();
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public Task NestedWithNullableNav_EmitsSplicedColumns()
    {
        // Arrange — Department is a nullable navigation; FN1006 today fires from
        // PropertyMappingExtractor at extraction time, not for resolver-spliced paths, so this
        // snapshot just pins the spliced columns. Extending FN1006 to spliced paths is a vNext
        // concern.
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
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }
}
