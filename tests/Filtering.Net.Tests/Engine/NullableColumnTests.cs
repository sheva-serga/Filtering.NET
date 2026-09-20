using System.Linq.Expressions;

using AwesomeAssertions;

using Xunit;

using static Filtering.Net.Tests.Engine.EngineTestData;

namespace Filtering.Net.Tests.Engine;

// Bob has null in every nullable column; the expectations are what C# lifted operators give.
public class NullableColumnTests
{
    [Theory]
    [InlineData("eq", "10", new[] { "Alice" })]
    [InlineData("ne", "10", new[] { "Bob", "Carol" })]
    [InlineData("gt", "5", new[] { "Alice", "Carol" })]
    [InlineData("lte", "10", new[] { "Alice" })]
    public void ApplyFilter_ComparisonOnNullableInt_MatchesLiftedSemantics(string operatorName, string valueJson, string[] expectedNames)
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("Score", operatorName, valueJson)).Names();

        // Assert
        filteredNames.Should().Equal(expectedNames);
    }

    [Fact]
    public void ApplyFilter_InOnNullableInt_SkipsNullRowsWithoutThrowing()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("Score", "in", "[10, 20]")).Names();

        // Assert
        filteredNames.Should().Equal("Alice", "Carol");
    }

    [Fact]
    public void ApplyFilter_InBoundToSpanContains_SkipsNullRowsWithoutThrowing()
    {
        // Arrange — compilers with first-class spans bind "values.Contains(column)" to
        // MemoryExtensions.Contains(ReadOnlySpan<T>, T) through an implicit array-to-span conversion.
        // This project's language version does not, so the tree is built by hand.
        var columnParameter = Expression.Parameter(typeof(int), "column");
        var valuesParameter = Expression.Parameter(typeof(int[]), "values");
        var arrayToSpan = typeof(ReadOnlySpan<int>).GetMethod("op_Implicit", [typeof(int[])])!;
        var spanContains = typeof(MemoryExtensions).GetMethods()
            .Single(method => method.Name == nameof(MemoryExtensions.Contains)
                && method.GetParameters().Length == 2
                && method.GetParameters()[0].ParameterType.Name == "ReadOnlySpan`1")
            .MakeGenericMethod(typeof(int));
        var spanBoundIn = Expression.Lambda<Func<int, int[], bool>>(
            Expression.Call(spanContains, Expression.Call(arrayToSpan, valuesParameter), columnParameter),
            columnParameter,
            valuesParameter);
        var profile = FilterProfile<int>.Create("SpanInt", FilterOperator.Value("in", spanBoundIn, Int32Filter.TryGetArray));
        var definition = Definition(properties: [FilterProperty.MapNullable<Person, int>("Score", person => person.Score, profile).Build()]);

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("Score", "in", "[10, 20]")).Names();

        // Assert
        filteredNames.Should().Equal("Alice", "Carol");
    }

    [Fact]
    public void ApplyFilter_IsNullOnNullableInt_ReturnsNullRows()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("Score", "isNull", "null")).Names();

        // Assert
        filteredNames.Should().Equal("Bob");
    }

    [Fact]
    public void ApplyFilter_IsNullOnNonNullableInt_ReturnsNothing()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("Age", "isNull", "null")).Names();

        // Assert
        filteredNames.Should().BeEmpty();
    }

    [Fact]
    public void ApplyFilter_ComparisonOnNullableShort_LiftsThroughTheCompilerInsertedWidening()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("Rank", "gte", "1")).Names();

        // Assert
        filteredNames.Should().Equal("Alice", "Carol");
    }

    [Fact]
    public void ApplyFilter_ComparisonOnNullableDateTime_LiftsUserDefinedOperator()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("LastSeen", "gt", "\"2026-03-01T00:00:00\"")).Names();

        // Assert
        filteredNames.Should().Equal("Carol");
    }

    [Fact]
    public void ApplyFilter_CustomOperatorUsingColumnMember_SplicesThroughConversion()
    {
        // Arrange
        var yearProfile = DateTimeFilter.Profile.Extend("YearFilter",
            FilterOperator.Value<DateTime, int>("year", (column, year) => column.Year == year, Int32Filter.TryGetValue));
        var definition = Definition(properties:
        [
            FilterProperty.Map<Person, int>("Age", person => person.Age, Int32Filter.Profile).Build(),
            FilterProperty.MapNullable<Person, DateTime>("LastSeen", person => person.LastSeen, yearProfile).Build(),
        ]);
        var rowsWithValue = Group(LogicalOp.And, Leaf("Age", "ne", "25"), Leaf("LastSeen", "year", "2026"));

        // Act
        var filteredNames = definition.ApplyFilter(People(), rowsWithValue).Names();

        // Assert
        filteredNames.Should().Equal("Alice", "Carol");
    }
}
