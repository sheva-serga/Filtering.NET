using System.Linq;

using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Resolution;

public class NestedFilterResolverTests
{
    [Fact]
    public void AutoResolve_UniqueCandidate_NoDiagnostics()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter { }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department))]
                private static partial void MapDepartment();
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void AutoResolve_TwoCandidates_FiresFN0019()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilterA { }
            [GenerateFilter<Department>] public partial class DepartmentFilterB { }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department))]
                private static partial void MapDepartment();
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0019");
    }

    [Fact]
    public void AutoResolve_NoCandidates_FiresFN0020()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department))]
                private static partial void MapDepartment();
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0020");
    }

    [Fact]
    public void NavigationDoesNotExist_FiresFN0021()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<Department>] public partial class DepartmentFilter { }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested("Department")]
                private static partial void MapDepartment();
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0021");
    }

    [Fact]
    public void NavigationIsCollection_FiresFN0022()
    {
        // Arrange
        var source = """
            using System.Collections.Generic;
            using Filtering.Net;
            namespace TestNs;
            public class Post { public string Title { get; set; } = ""; }
            public class User { public List<Post> Posts { get; set; } = new(); }
            [GenerateFilter<Post>] public partial class PostFilter { }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Posts))]
                private static partial void MapPosts();
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0022");
    }

    [Fact]
    public void Generic_NoMatchingFilterClass_FiresFN0018()
    {
        // Arrange
        // FakeFilter has no [GenerateFilter<>], so it never appears as a host extraction result.
        // The resolver's explicit-filter-class lookup misses and FN0018 fires.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            public class FakeFilter { }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested<FakeFilter>(nameof(User.Department))]
                private static partial void MapDepartment();
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0018");
    }

    [Fact]
    public void Splice_SinglyNested_AddsPrefixedMappings()
    {
        // Arrange
        var source = """
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
                [MapNested(nameof(User.Department))] private static partial void MapDept();
            }
            """;

        // Act
        var hostModel = ResolutionTestHelpers.Resolve(source, "UserFilter").Model;

        // Assert
        var paths = hostModel.Properties.Select(p => p.PropertyName).OrderBy(s => s).ToList();
        paths.Should().BeEquivalentTo(new[] { "Department.Id", "Department.Name" });
    }

    [Fact]
    public void Splice_OnlyRestricts_DropsExcludedPaths()
    {
        // Arrange
        var source = """
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
        var hostModel = ResolutionTestHelpers.Resolve(source, "UserFilter").Model;

        // Assert
        hostModel.Properties.Select(p => p.PropertyName).Should().BeEquivalentTo(new[] { "Department.Id" });
    }

    [Fact]
    public void Splice_DisableSorting_OverridesSourceSortable()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [Map(nameof(Department.Name), Sortable = true)] private static partial void MapName();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department), DisableSorting = true)]
                private static partial void MapDept();
            }
            """;

        // Act
        var hostModel = ResolutionTestHelpers.Resolve(source, "UserFilter").Model;

        // Assert
        hostModel.Properties.Single(p => p.PropertyName == "Department.Name").Sortable.Should().BeFalse();
    }

    [Fact]
    public void Splice_TransitiveTwoLevel_FlattensPaths()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Company { public string Name { get; set; } = ""; }
            public class Department { public string Name { get; set; } = ""; public Company Company { get; set; } = new(); }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Company>] public partial class CompanyFilter
            {
                [Map(nameof(Company.Name))] private static partial void MapName();
            }
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
                [Map(nameof(Department.Name))] private static partial void MapName();
                [MapNested(nameof(Department.Company))] private static partial void MapCompany();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department))] private static partial void MapDept();
            }
            """;

        // Act
        var hostModel = ResolutionTestHelpers.Resolve(source, "UserFilter").Model;

        // Assert
        var paths = hostModel.Properties.Select(p => p.PropertyName).OrderBy(s => s).ToList();
        paths.Should().BeEquivalentTo(new[] { "Department.Name", "Department.Company.Name" });
    }

}
