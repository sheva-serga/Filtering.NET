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

    [Fact]
    public void ExtendWithOverrides_RedeclaredInheritedOperator_ReplacesItAndKeepsTheRestOfTheBase()
    {
        // Arrange
        var caselessEquality = FilterOperator.Value<string, string>(
            "eq", (column, value) => column.ToLower() == value.ToLower(), StringFilter.TryGetValue);

        // Act
        var derivedProfile = StringFilter.Profile.ExtendWithOverrides("CaselessStringFilter", caselessEquality);

        // Assert
        derivedProfile.Operators["eq"].Should().BeSameAs(caselessEquality);
        derivedProfile.Operators.Keys.Should().BeEquivalentTo(StringFilter.Profile.Operators.Keys);
        StringFilter.Profile.Operators["eq"].Should().NotBeSameAs(caselessEquality);
    }

    [Fact]
    public void ExtendWithOverrides_RedeclaredInheritedOperatorDifferingOnlyInCase_ReplacesIt()
    {
        // Arrange
        var caselessEquality = FilterOperator.Value<string, string>(
            "EQ", (column, value) => column.ToLower() == value.ToLower(), StringFilter.TryGetValue);

        // Act
        var derivedProfile = StringFilter.Profile.ExtendWithOverrides("CaselessStringFilter", caselessEquality);

        // Assert
        derivedProfile.Operators.Should().HaveCount(StringFilter.Profile.Operators.Count);
        derivedProfile.Operators["eq"].Should().BeSameAs(caselessEquality);
    }

    [Fact]
    public void ExtendWithOverrides_SameOperatorNameTwiceInOneCall_ThrowsConfigurationException()
    {
        // Act
        var extendWithDuplicate = () => StringFilter.Profile.ExtendWithOverrides("Broken",
            FilterOperator.Value<string, string>("fuzzy", (column, value) => column.Contains(value), StringFilter.TryGetValue),
            FilterOperator.Value<string, string>("FUZZY", (column, value) => column.StartsWith(value), StringFilter.TryGetValue));

        // Assert
        extendWithDuplicate.Should().Throw<FilterConfigurationException>().WithMessage("*'FUZZY' more than once*");
    }

    [Fact]
    public void ApplyFilter_ProfileThatOverrodeAnInheritedOperator_UsesTheOverride()
    {
        // Arrange
        var caselessProfile = StringFilter.Profile.ExtendWithOverrides("CaselessStringFilter",
            FilterOperator.Value<string, string>("eq", (column, value) => column.ToLower() == value.ToLower(), StringFilter.TryGetValue));
        var definition = EngineTestData.Definition(properties:
        [
            FilterProperty.Map<Person, string>("Name", person => person.Name, caselessProfile).Build(),
        ]);

        // Act
        var filteredNames = definition.ApplyFilter(EngineTestData.People(), EngineTestData.Leaf("Name", "eq", "\"alice\"")).Names();

        // Assert
        filteredNames.Should().Equal("Alice");
    }

    // Named by type, so a built-in profile that stops being compiled into the shipped assembly
    // breaks this file. The reflection test below closes the other direction: a profile the
    // assembly ships that nobody listed here.
    private static readonly Type[] BuiltInProfileTypes =
    [
        typeof(StringFilter),
        typeof(BoolFilter),
        typeof(GuidFilter),
        typeof(DateTimeFilter),
        typeof(DateTimeOffsetFilter),
        typeof(DateOnlyFilter),
        typeof(TimeOnlyFilter),
        typeof(ByteFilter),
        typeof(Int16Filter),
        typeof(Int32Filter),
        typeof(Int64Filter),
        typeof(DecimalFilter),
        typeof(DoubleFilter),
        typeof(SingleFilter),
    ];

    public static TheoryData<Type, string[]> BuiltInProfiles
    {
        get
        {
            var builtInProfiles = new TheoryData<Type, string[]>();
            foreach (var builtInProfileType in BuiltInProfileTypes)
            {
                builtInProfiles.Add(builtInProfileType, RuntimeOperatorNames(builtInProfileType));
            }
            return builtInProfiles;
        }
    }

    [Fact]
    public void BuiltInProfiles_ListedTypes_MatchEveryFilterProfileTheRuntimeAssemblyShips()
    {
        // Arrange
        var shippedProfileTypes = typeof(StringFilter).Assembly.GetTypes()
            .Where(candidateType => candidateType.IsPublic
                && candidateType.IsAbstract
                && candidateType.IsSealed
                && candidateType.GetCustomAttributesData().Any(attributeData =>
                    attributeData.AttributeType.IsGenericType
                    && attributeData.AttributeType.GetGenericTypeDefinition() == typeof(FilterProfileAttribute<>)));

        // Act
        var listedProfileTypes = BuiltInProfileTypes;

        // Assert
        listedProfileTypes.Should().BeEquivalentTo(shippedProfileTypes);
    }

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

    private static string[] RuntimeOperatorNames(Type builtInProfileType)
    {
        var profile = builtInProfileType.GetProperty("Profile", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var operatorsByName = (System.Collections.IEnumerable)profile.GetType().GetProperty("Operators")!.GetValue(profile)!;
        return [.. operatorsByName.Cast<object>()
            .Select(operatorEntry => (string)operatorEntry.GetType().GetProperty("Key")!.GetValue(operatorEntry)!)];
    }
}
