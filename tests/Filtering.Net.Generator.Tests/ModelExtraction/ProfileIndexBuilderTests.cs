using AwesomeAssertions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Filtering.Net.Generator.Tests.ModelExtraction;

public class ProfileIndexBuilderTests
{
    internal static Compilation Compile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location),
        };
        return CSharpCompilation.Create("test", [tree], references);
    }

    [Fact]
    public void Build_SourceWithBuiltInIntProfile_DiscoversProfile()
    {
        // Arrange
        var source = @"
namespace Filtering.Net {
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class FilterProfileAttribute<T> : System.Attribute { }
    [FilterProfile<int>] public static class Int32Filter {}
}";
        var compilation = Compile(source);

        // Act
        var profileIndex = ProfileIndexBuilder.Build(compilation);

        // Assert
        profileIndex.Lookup("int").Should().Equal("Filtering.Net.Int32Filter");
    }

    [Fact]
    public void Build_TwoProfilesForSameType_KeepsBoth()
    {
        // Arrange
        var source = @"
namespace Filtering.Net {
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class FilterProfileAttribute<T> : System.Attribute { }
    [FilterProfile<int>] public static class Int32Filter {}
}
namespace Sample {
    [Filtering.Net.FilterProfile<int>] public static class MyIntFilter {}
}";
        var compilation = Compile(source);

        // Act
        var profileIndex = ProfileIndexBuilder.Build(compilation);
        var matches = profileIndex.Lookup("int");

        // Assert
        matches.Should().HaveCount(2);
        matches.Should().Contain("Filtering.Net.Int32Filter");
        matches.Should().Contain("Sample.MyIntFilter");
    }

    [Fact]
    public void ResolveCandidates_BuiltInIntProfile_ReturnsOneMatch()
    {
        // Arrange
        var source = @"
namespace Filtering.Net {
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class FilterProfileAttribute<T> : System.Attribute { }
    [FilterProfile<int>] public static class Int32Filter {}
}";
        var compilation = Compile(source);
        var profileIndex = ProfileIndexBuilder.Build(compilation);
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);

        // Act
        var resolutionResult = ProfileResolver.ResolveCandidates(intType, profileIndex);

        // Assert
        resolutionResult.Count.Should().Be(1);
        resolutionResult.ProfileFullNames[0].Should().Be("Filtering.Net.Int32Filter");
    }
}
