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
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void AutoResolve_TwoCandidates_FiresFN0017()
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
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0017");
    }

    [Fact]
    public void AutoResolve_NoCandidates_FiresFN0018()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0018");
    }

    [Fact]
    public void NavigationDoesNotExist_FiresFN0019()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<Department>] public partial class DepartmentFilter { }
            [GenerateFilter<User>]
            [MapNested("Department")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0019");
    }

    [Fact]
    public void NavigationIsCollection_FiresFN0020()
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
            [MapNested(nameof(User.Posts))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0020");
    }

    [Fact]
    public void Generic_NoMatchingFilterClass_FiresFN0016()
    {
        // Arrange
        // FakeFilter has no [GenerateFilter<>], so it never appears as a host extraction result.
        // The resolver's explicit-filter-class lookup misses and FN0016 fires.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            public class FakeFilter { }
            [GenerateFilter<User>]
            [MapNested<FakeFilter>(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var resolved = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        resolved.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0016");
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
            [Map(nameof(Department.Id))]
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
            [Map(nameof(Company.Name))]
            [GenerateFilter<Company>] public partial class CompanyFilter
            {
            }
            [Map(nameof(Department.Name))]
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
        var hostModel = ResolutionTestHelpers.Resolve(source, "UserFilter").Model;

        // Assert
        var paths = hostModel.Properties.Select(p => p.PropertyName).OrderBy(s => s).ToList();
        paths.Should().BeEquivalentTo(new[] { "Department.Name", "Department.Company.Name" });
    }

}
