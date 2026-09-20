using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0020Tests
{
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
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "FN0020");
    }

    [Fact]
    public void SingleReferenceNavigation_DoesNotFireFN0020()
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
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0020");
    }

    [Fact]
    public void NestedCollectionUnsupported_ReportsCollectionNavigationAsAdditionalLocation()
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
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0020", expectedAdditionalCount: 1);
    }
}
