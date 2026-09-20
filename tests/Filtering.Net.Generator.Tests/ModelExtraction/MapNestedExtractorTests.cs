using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.ModelExtraction;

public class MapNestedExtractorTests
{
    [Fact]
    public void NoMapNested_ProducesEmptyList()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var model = ExtractModel(source);

        // Assert
        model.NestedMappings.Should().BeEmpty();
    }

    [Fact]
    public void NonGeneric_AutoResolve_PopulatesModel()
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
        var model = ExtractModel(source, filterClassName: "UserFilter");

        // Assert
        model.NestedMappings.Should().ContainSingle();
        var nested = model.NestedMappings[0];
        nested.NavigationPropertyName.Should().Be("Department");
        nested.Prefix.Should().Be("Department");
        nested.ExplicitFilterClassFqn.Should().BeNull();
        nested.Only.Should().BeEmpty();
        nested.Except.Should().BeEmpty();
        nested.DisableSorting.Should().BeFalse();
    }

    [Fact]
    public void Generic_ExplicitFilterClass_PopulatesFqn()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter { }
            [GenerateFilter<User>]
            [MapNested<DepartmentFilter>(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var model = ExtractModel(source, filterClassName: "UserFilter");

        // Assert
        model.NestedMappings.Should().ContainSingle();
        model.NestedMappings[0].ExplicitFilterClassFqn.Should().Be("TestNs.DepartmentFilter");
    }

    [Fact]
    public void AllProperties_StoredOnModel()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter { }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department), Prefix = "dept", Only = new[] { "id", "name" }, Except = new[] { "internal" }, DisableSorting = true)]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var model = ExtractModel(source, filterClassName: "UserFilter");

        // Assert
        var nested = model.NestedMappings[0];
        nested.Prefix.Should().Be("dept");
        nested.Only.Should().BeEquivalentTo(new[] { "id", "name" });
        nested.Except.Should().BeEquivalentTo(new[] { "internal" });
        nested.DisableSorting.Should().BeTrue();
    }

    private static FilterClassModel ExtractModel(string source, string? filterClassName = null)
    {
        var models = GeneratorRunner.ExtractFilterClassModels(source, excludeDiAbstractions: false);
        if (filterClassName is null)
        {
            return models.Should().ContainSingle().Subject;
        }
        return models.Should().Contain(extracted => extracted.ClassName == filterClassName).Subject;
    }
}
