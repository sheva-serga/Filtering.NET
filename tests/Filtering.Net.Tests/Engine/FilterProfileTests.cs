using System.Reflection;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Engine;

public class FilterProfileTests
{
    [Fact]
    public void Create_DuplicateOperatorNameIgnoringCase_ThrowsConfigurationException()
    {
        // Act
        var createWithDuplicate = () => FilterProfile<int>.Create("Broken",
            FilterOperator.Value<int, int>("eq", (column, value) => column == value, Int32Filter.TryGetValue),
            FilterOperator.Value<int, int>("EQ", (column, value) => column == value, Int32Filter.TryGetValue));

        // Assert
        createWithDuplicate.Should().Throw<FilterConfigurationException>().WithMessage("*'EQ' more than once*");
    }

    [Fact]
    public void Operators_LookupWithDifferentCase_FindsOperator()
    {
        // Act
        var hasStartsWith = StringFilter.Profile.Operators.ContainsKey("STARTSWITH");

        // Assert
        hasStartsWith.Should().BeTrue();
    }

    [Fact]
    public void Extend_AddedOperator_InheritsBaseOperatorsAndLeavesBaseUntouched()
    {
        // Act
        var extendedProfile = StringFilter.Profile.Extend("StringFilterPlus",
            FilterOperator.Value<string, string>("fuzzy", (column, value) => column.ToLower().Contains(value.ToLower()), StringFilter.TryGetValue));

        // Assert
        extendedProfile.Name.Should().Be("StringFilterPlus");
        extendedProfile.Operators.Keys.Should().Contain(["eq", "contains", "fuzzy"]);
        StringFilter.Profile.Operators.Keys.Should().NotContain("fuzzy");
    }

    [Fact]
    public void Extend_RedeclaredInheritedOperator_ThrowsConfigurationException()
    {
        // Act
        var extendWithDuplicate = () => StringFilter.Profile.Extend("Broken",
            FilterOperator.Value<string, string>("eq", (column, value) => column == value, StringFilter.TryGetValue));

        // Assert
        extendWithDuplicate.Should().Throw<FilterConfigurationException>();
    }

    public static TheoryData<Type, string[]> BuiltInProfiles => new()
    {
        { typeof(StringFilter), [.. StringFilter.Profile.Operators.Keys] },
        { typeof(BoolFilter), [.. BoolFilter.Profile.Operators.Keys] },
        { typeof(GuidFilter), [.. GuidFilter.Profile.Operators.Keys] },
        { typeof(DateTimeFilter), [.. DateTimeFilter.Profile.Operators.Keys] },
        { typeof(DateTimeOffsetFilter), [.. DateTimeOffsetFilter.Profile.Operators.Keys] },
        { typeof(ByteFilter), [.. ByteFilter.Profile.Operators.Keys] },
        { typeof(Int16Filter), [.. Int16Filter.Profile.Operators.Keys] },
        { typeof(Int32Filter), [.. Int32Filter.Profile.Operators.Keys] },
        { typeof(Int64Filter), [.. Int64Filter.Profile.Operators.Keys] },
        { typeof(DecimalFilter), [.. DecimalFilter.Profile.Operators.Keys] },
        { typeof(DoubleFilter), [.. DoubleFilter.Profile.Operators.Keys] },
        { typeof(SingleFilter), [.. SingleFilter.Profile.Operators.Keys] },
    };

    [Theory]
    [MemberData(nameof(BuiltInProfiles))]
    public void Profile_BuiltIn_ExposesExactlyTheDeclaredFilterOperators(Type builtInProfileType, string[] runtimeOperatorNames)
    {
        // Arrange
        var declaredOperatorNames = builtInProfileType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(property => property.GetCustomAttribute<FilterOperatorAttribute>()?.Name)
            .Where(operatorName => operatorName is not null);

        // Assert
        runtimeOperatorNames.Should().BeEquivalentTo(declaredOperatorNames);
    }
}
