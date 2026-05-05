using AwesomeAssertions;

using Filtering.Net.Generator.Tests.ModelExtraction;

using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator.Tests.Discovery;

public class EnumTypeCollectorTests
{
    [Fact]
    public void Collect_EntityWithMultipleEnumPropertiesSomeShared_FindsEachDistinctEnumOnce()
    {
        // Arrange
        var source = @"
namespace Filtering.Net {
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class GenerateFilterAttribute<TEntity> : System.Attribute { }
}
namespace Sample {
    public enum UserStatus { Active, Closed }
    public enum UserKind { Admin, Member }
    public class User {
        public UserStatus Status { get; set; }
        public UserKind Kind { get; set; }
        public UserStatus AnotherStatus { get; set; }
    }
    [Filtering.Net.GenerateFilter<User>] public partial class UserFilter {}
}";
        var compilation = ProfileIndexBuilderTests.Compile(source);

        // Act
        var enumNames = EnumTypeCollector.Collect(compilation)
            .Select(t => t.ToDisplayString())
            .OrderBy(n => n)
            .ToArray();

        // Assert
        enumNames.Should().Equal("Sample.UserKind", "Sample.UserStatus");
    }
}
