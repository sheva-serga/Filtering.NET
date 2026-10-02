using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>
/// Every wire-key collision shape maps to exactly one diagnostic id: a collision purely among the
/// host's own [Map]/[PropertyMap] names and aliases is FN0009; anything involving a spliced or lifted
/// key, or the same CLR path, is FN0001.
/// </summary>
public class WireKeyCollisionMatrixTests
{
    [Fact]
    public void TwoMapsWithTheSameName_ReportsOnlyFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0001");
    }

    [Fact]
    public void TwoMapsWithTheSameNameAndAlias_ReportsOnlyFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Alias = "title")]
            [Map(nameof(User.Name), Alias = "title")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0001");
    }

    [Fact]
    public void MapAliasEqualToAnotherMapName_ReportsOnlyFN0009()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public string Nickname { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [Map(nameof(User.Nickname), Alias = "name")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0009");
    }

    [Fact]
    public void TwoMapsWithTheSameAlias_ReportsOnlyFN0009()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public string Nickname { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Alias = "title")]
            [Map(nameof(User.Nickname), Alias = "TITLE")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0009");
    }

    [Fact]
    public void MapAliasEqualToPropertyMapName_ReportsOnlyFN0009()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Nickname { get; set; } = ""; public string First { get; set; } = ""; public string Last { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Nickname), Alias = "fullName")]
            public partial class UserFilter
            {
                [PropertyMap("FullName")]
                private static FilterRule<User, string> MapFullName(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.First + " " + user.Last);
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0009");
    }

    [Fact]
    public void PropertyMapAliasEqualToMapName_ReportsOnlyFN0009()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public string First { get; set; } = ""; public string Last { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
                [PropertyMap("FullName", Alias = "name")]
                private static FilterRule<User, string> MapFullName(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.First + " " + user.Last);
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0009");
    }

    [Fact]
    public void PropertyMapAliasEqualToAnotherPropertyMapName_ReportsOnlyFN0009()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string First { get; set; } = ""; public string Last { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [PropertyMap("FullName")]
                private static FilterRule<User, string> MapFullName(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.First + " " + user.Last);

                [PropertyMap("Initials", Alias = "fullName")]
                private static FilterRule<User, string> MapInitials(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.First.Substring(0, 1) + user.Last.Substring(0, 1));
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0009");
    }

    [Fact]
    public void TwoPropertyMapsWithTheSameAlias_ReportsOnlyFN0009()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string First { get; set; } = ""; public string Last { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [PropertyMap("FullName", Alias = "label")]
                private static FilterRule<User, string> MapFullName(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.First + " " + user.Last);

                [PropertyMap("Initials", Alias = "label")]
                private static FilterRule<User, string> MapInitials(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.First.Substring(0, 1) + user.Last.Substring(0, 1));
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0009");
    }

    [Fact]
    public void TwoPropertyMapsWithTheSameName_ReportsOnlyFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Email { get; set; } = ""; public string Login { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [PropertyMap("Email")]
                private static FilterRule<User, string> RuleA(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.Email);

                [PropertyMap("Email")]
                private static FilterRule<User, string> RuleB(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.Login);
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0001");
    }

    [Fact]
    public void HostMapPathEqualToSplicedPath_ReportsOnlyFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>]
            [Map(nameof(Department.Name))]
            public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [Map("Department.Name")]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0001");
    }

    [Fact]
    public void HostMapAliasEqualToSplicedKey_ReportsOnlyFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); public string Nickname { get; set; } = ""; }
            [GenerateFilter<Department>]
            [Map(nameof(Department.Name))]
            public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [Map(nameof(User.Nickname), Alias = "Department.Name")]
            [MapNested(nameof(User.Department), Prefix = "Dept")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0001");
    }

    [Fact]
    public void HostPropertyMapAliasEqualToSplicedKey_ReportsOnlyFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); public string First { get; set; } = ""; public string Last { get; set; } = ""; }
            [GenerateFilter<Department>]
            [Map(nameof(Department.Name))]
            public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department), Prefix = "Dept")]
            public partial class UserFilter
            {
                [PropertyMap("Label", Alias = "Department.Name")]
                private static FilterRule<User, string> MapLabel(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.First + " " + user.Last);
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0001");
    }

    [Fact]
    public void TwoSplicesProducingTheSameKey_ReportsOnlyFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class Office { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); public Office Office { get; set; } = new(); }
            [Map(nameof(Department.Name))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [Map(nameof(Office.Name))]
            [GenerateFilter<Office>] public partial class OfficeFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            [MapNested(nameof(User.Office), Prefix = "department")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0001");
    }

    [Fact]
    public void LiftedPropertyMapAliasEqualToHostMapPath_ReportsOnlyFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; public string Code { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>]
            [Map(nameof(Department.Code))]
            public partial class DepartmentFilter
            {
                [PropertyMap("FullName", Alias = "name")]
                private static FilterRule<Department, string> MapFullName(FilterRuleBuilder<Department, string> builder) =>
                    builder.For(department => department.Name + " (" + department.Code + ")");
            }
            [GenerateFilter<User>]
            [Map("Department.Name")]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0001");
    }

    [Fact]
    public void LiftedPropertyMapAliasEqualToHostMapAlias_ReportsOnlyFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; public string Code { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); public string Nickname { get; set; } = ""; }
            [GenerateFilter<Department>]
            [Map(nameof(Department.Code))]
            public partial class DepartmentFilter
            {
                [PropertyMap("FullName", Alias = "name")]
                private static FilterRule<Department, string> MapFullName(FilterRuleBuilder<Department, string> builder) =>
                    builder.For(department => department.Name + " (" + department.Code + ")");
            }
            [GenerateFilter<User>]
            [Map(nameof(User.Nickname), Alias = "department.name")]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        AssertExactIds(source, "FN0001");
    }

    private static void AssertExactIds(string source, params string[] expectedIds)
    {
        var observedIds = DiagnosticTestHelpers.GetDiagnostics(source)
            .Select(diagnostic => diagnostic.Id)
            .Distinct()
            .ToList();

        observedIds.Should().BeEquivalentTo(expectedIds);
    }
}
