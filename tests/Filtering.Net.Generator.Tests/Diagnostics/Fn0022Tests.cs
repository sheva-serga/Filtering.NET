using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0022Tests
{
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
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "FN0022");
    }

    [Fact]
    public void SingleReferenceNavigation_DoesNotFireFN0022()
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
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0022");
    }
}
