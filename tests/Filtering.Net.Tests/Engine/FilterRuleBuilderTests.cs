using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using AwesomeAssertions;

using Xunit;

using static Filtering.Net.Tests.Engine.EngineTestData;

namespace Filtering.Net.Tests.Engine;

public class FilterRuleBuilderTests
{
    [Fact]
    public void ImplicitConversion_ForAndOperators_ProducesRuleUsableAsProperty()
    {
        // Arrange
        FilterRule<Person, string> rule = new FilterRuleBuilder<Person, string>()
            .For(person => person.Name + "@" + person.Department.Name)
            .Operator<string>("endsWith", (column, suffix) => column.EndsWith(suffix))
            .Operator("isShort", column => column.Length < 10);
        var serializerOptions = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        var definition = Definition(serializerOptions: serializerOptions, properties: [FilterProperty.MapRule("Handle", rule).Build()]);

        // Act
        var bySuffix = definition.ApplyFilter(People(), Leaf("Handle", "endsWith", "\"@Sales\"")).Names();
        var byLength = definition.ApplyFilter(People(), Leaf("handle", "isShort", "null")).Names();

        // Assert
        rule.Operators.Select(filterOperator => filterOperator.Name).Should().Equal("endsWith", "isShort");
        bySuffix.Should().Equal("Alice", "Carol");
        byLength.Should().Equal("Bob");
    }

    [Fact]
    public void ImplicitConversion_WithoutFor_ThrowsConfigurationException()
    {
        // Arrange
        var builderWithoutAccessor = new FilterRuleBuilder<Person, string>().Operator("isShort", column => column.Length < 10);

        // Act
        var convert = () => { FilterRule<Person, string> _ = builderWithoutAccessor; };

        // Assert
        convert.Should().Throw<FilterConfigurationException>().WithMessage("*For(...)*");
    }

    [Fact]
    public void For_CalledTwice_ThrowsConfigurationException()
    {
        // Arrange
        var builder = new FilterRuleBuilder<Person, string>().For(person => person.Name);

        // Act
        var declareSecondAccessor = () => builder.For(person => person.Department.Name);

        // Assert
        declareSecondAccessor.Should().Throw<FilterConfigurationException>().WithMessage("*For(...)*");
    }

    [Fact]
    public void Operator_DuplicateName_ThrowsConfigurationException()
    {
        // Arrange
        var builder = new FilterRuleBuilder<Person, string>().Operator("isShort", column => column.Length < 10);

        // Act
        var addDuplicate = () => builder.Operator<int>("ISSHORT", (column, length) => column.Length < length);

        // Assert
        addDuplicate.Should().Throw<FilterConfigurationException>();
    }
}
