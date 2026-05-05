using AwesomeAssertions;
using System.Linq.Expressions;
using Xunit;

namespace Filtering.Net.Tests.Composition;

public class PredicateBuilderTests
{
    private record TestEntity(int Age, string Name);

    [Fact]
    public void AndAlso_TwoPredicates_ReturnsTrueOnlyWhenBothMatch()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> isAdult = entity => entity.Age >= 18;
        Expression<Func<TestEntity, bool>> nameStartsWithJ = entity => entity.Name.StartsWith("J");

        // Act
        var combined = isAdult.AndAlso(nameStartsWithJ);
        var compiled = combined.Compile();

        // Assert
        compiled(new TestEntity(20, "John")).Should().BeTrue();
        compiled(new TestEntity(20, "Alice")).Should().BeFalse();
        compiled(new TestEntity(15, "John")).Should().BeFalse();
    }

    [Fact]
    public void OrElse_TwoPredicates_ReturnsTrueWhenEitherMatches()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> isYoung = entity => entity.Age < 18;
        Expression<Func<TestEntity, bool>> isOld = entity => entity.Age > 60;

        // Act
        var combined = isYoung.OrElse(isOld);
        var compiled = combined.Compile();

        // Assert
        compiled(new TestEntity(10, "")).Should().BeTrue();
        compiled(new TestEntity(70, "")).Should().BeTrue();
        compiled(new TestEntity(40, "")).Should().BeFalse();
    }

    [Fact]
    public void Not_SinglePredicate_InvertsResult()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> isAdult = entity => entity.Age >= 18;

        // Act
        var negated = isAdult.Not();
        var compiled = negated.Compile();

        // Assert
        compiled(new TestEntity(20, "")).Should().BeFalse();
        compiled(new TestEntity(15, "")).Should().BeTrue();
    }

    [Fact]
    public void AndAlso_TwoPredicates_ProducesSingleParameterExpressionForEFCompatibility()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> first = entity => entity.Age > 10;
        Expression<Func<TestEntity, bool>> second = entity => entity.Name == "x";

        // Act
        var combined = first.AndAlso(second);

        // Assert
        combined.Parameters.Should().HaveCount(1);
        combined.Parameters[0].Type.Should().Be<TestEntity>();
    }
}
