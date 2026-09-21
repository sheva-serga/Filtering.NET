using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using AwesomeAssertions;

using Xunit;

using static Filtering.Net.Tests.Engine.EngineTestData;

namespace Filtering.Net.Tests.Engine;

public class FilterPropertyTests
{
    [Fact]
    public void Build_OnlyAndExcept_NarrowOperatorsInProfileOrder()
    {
        // Act
        var property = FilterProperty.Map<Person, string>("Name", person => person.Name, StringFilter.Profile)
            .Only("IN", "eq", "contains")
            .Except("contains")
            .Build();

        // Assert
        property.Operators.Should().Equal("eq", "in");
        property.ProfileName.Should().Be("StringFilter");
    }

    [Fact]
    public void Build_OnlyNamesUnknownOperator_ThrowsConfigurationException()
    {
        // Act
        var buildWithUnknownOperator = () => FilterProperty.Map<Person, int>("Age", person => person.Age, Int32Filter.Profile)
            .Only("contains")
            .Build();

        // Assert
        buildWithUnknownOperator.Should().Throw<FilterConfigurationException>().WithMessage("*'contains'*Int32Filter*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Alias_BlankAlias_ThrowsConfigurationException(string? alias)
    {
        // Act
        var aliasWithBlank = () => FilterProperty.Map<Person, string>("Name", person => person.Name, StringFilter.Profile)
            .Alias(alias!);

        // Assert
        aliasWithBlank.Should().Throw<FilterConfigurationException>().WithMessage("*'Name'*");
    }

    [Fact]
    public void Validate_OperatorExcludedByOnly_ReportsOperatorNotAllowed()
    {
        // Arrange
        var definition = Definition(properties: [FilterProperty.Map<Person, string>("Name", person => person.Name, StringFilter.Profile).Only("eq").Build()]);

        // Act
        var validationResult = definition.Validate(Leaf("Name", "contains", "\"a\""));

        // Assert
        validationResult.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.OperatorNotAllowed);
    }

    [Fact]
    public void ApplyFilter_ScalarInterceptor_TransformsValueAndSeesContext()
    {
        // Arrange
        InterceptContext? observedContext = null;
        var definition = Definition(properties:
        [
            FilterProperty.Map<Person, string>("Name", person => person.Name, StringFilter.Profile)
                .Alias("fullName")
                .Intercept((interceptContext, value) =>
                {
                    observedContext = interceptContext;
                    return value.Trim();
                })
                .Build(),
        ]);

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("fullName", "eq", "\"  Bob \"")).Names();

        // Assert
        filteredNames.Should().Equal("Bob");
        observedContext.Should().Be(new InterceptContext("Name", "fullName", "eq"));
    }

    [Fact]
    public void ApplyFilter_ArrayInterceptor_TransformsInValues()
    {
        // Arrange
        var definition = Definition(properties:
        [
            FilterProperty.Map<Person, int>("Age", person => person.Age, Int32Filter.Profile)
                .InterceptArray((_, values) => [.. values.Select(value => value + 5)])
                .Build(),
        ]);

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("Age", "in", "[20]")).Names();

        // Assert
        filteredNames.Should().Equal("Bob");
    }

    [Fact]
    public void ApplyFilter_RawInterceptor_ReplacesParsing()
    {
        // Arrange
        var definition = Definition(properties:
        [
            FilterProperty.Map<Person, int>("Age", person => person.Age, Int32Filter.Profile)
                .InterceptRaw((_, element) => element.GetString() == "thirty" ? 30 : -1)
                .Build(),
        ]);
        var leaf = Leaf("Age", "eq", "\"thirty\"");

        // Act
        var validationResult = definition.Validate(leaf);
        var filteredNames = definition.ApplyFilter(People(), leaf).Names();

        // Assert
        validationResult.IsValid.Should().BeTrue();
        filteredNames.Should().Equal("Alice");
    }

    [Fact]
    public void Validate_InterceptorThrowsValidationException_ReportsInterceptorRejectedWithFirstMessage()
    {
        // Arrange
        var rejection = new FilterValidationResult([new FilterValidationError("ignored", FilterValidationCode.InvalidValueType, "Name is banned.")]);
        var definition = Definition(properties:
        [
            FilterProperty.Map<Person, string>("Name", person => person.Name, StringFilter.Profile)
                .Intercept((_, _) => throw new FilterValidationException(rejection))
                .Build(),
        ]);

        // Act
        var validationResult = definition.Validate(Leaf("Name", "eq", "\"x\""));

        // Assert
        var validationError = validationResult.Errors.Should().ContainSingle().Subject;
        validationError.Code.Should().Be(FilterValidationCode.InterceptorRejected);
        validationError.Path.Should().Be("where.value");
        validationError.Message.Should().Be("Name is banned.");
    }

    [Fact]
    public void Validate_InterceptorThrowsNonValidationException_PropagatesItToTheCaller()
    {
        // Arrange — the documented way for an interceptor to reject a value is FilterValidationException.
        // Anything else is a defect in the interceptor, and the engine deliberately does not swallow it
        // into a validation error: that would hide the bug behind a 400.
        var definition = Definition(properties:
        [
            FilterProperty.Map<Person, int>("Age", person => person.Age, Int32Filter.Profile)
                .InterceptRaw((_, element) => int.Parse(element.GetString()!))
                .Build(),
        ]);

        // Act
        var validateWithFailingInterceptor = () => definition.Validate(Leaf("Age", "eq", "\"abc\""));

        // Assert
        validateWithFailingInterceptor.Should().Throw<FormatException>();
    }

    [Fact]
    public void LiftInto_DefaultPrefix_ExposesPrefixedFieldsAndKeepsBehaviour()
    {
        // Arrange
        var departmentSchema = new FilterSchemaBuilder<Department>(new FilterSettings())
            .Add(FilterProperty.Map<Department, string>("Name", department => department.Name, StringFilter.Profile).Sortable().Build())
            .Build();
        var schema = new FilterSchemaBuilder<Person>(new FilterSettings())
            .AddRange(departmentSchema.LiftInto<Person>(person => person.Department, "Department"))
            .Build();
        var definition = new FilterDefinition<Person>(schema);

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("department.name", "eq", "\"Ops\"")).Names();
        var sortedNames = definition.ApplySorting(People(), [new SortItem("Department.Name")]).Names();

        // Assert
        schema.Properties.Should().ContainSingle().Which.Field.Should().Be("Department.Name");
        schema.Properties[0].Alias.Should().BeNull();
        filteredNames.Should().Equal("Bob");
        sortedNames[0].Should().Be("Bob");
    }

    [Fact]
    public void LiftInto_CustomPrefixOnlyAndDisableSorting_AppliesToDirectPropertiesAndKeepsTransitiveOnes()
    {
        // Arrange
        var departmentSchema = DepartmentSchemaWithLiftedCompany();

        // Act
        var liftedProperties = departmentSchema.LiftInto<Person>(
            person => person.Department, "dept", only: ["Id"], except: null, disableSorting: true);

        // Assert
        liftedProperties.Select(property => property.Field).Should().Equal("Department.Id", "Department.Company.Country");
        liftedProperties.Select(property => property.Alias).Should().Equal("dept.Id", "dept.Company.Country");
        liftedProperties.Should().AllSatisfy(property => property.Sortable.Should().BeFalse());
    }

    [Fact]
    public void LiftInto_Except_DropsTheNamedPropertyAndKeepsTheOtherDirectOnes()
    {
        // Arrange
        var departmentSchema = DepartmentSchemaWithLiftedCompany();

        // Act
        var liftedProperties = departmentSchema.LiftInto<Person>(
            person => person.Department, "Department", only: null, except: ["Name"]);

        // Assert — both halves of the except predicate: 'Name' is dropped, 'Id' survives.
        liftedProperties.Select(property => property.Field).Should().Equal("Department.Id", "Department.Company.Country");
    }

    [Fact]
    public void LiftInto_ExceptedPath_IsNotFilterableOnTheHost()
    {
        // Arrange
        var departmentSchema = new FilterSchemaBuilder<Department>(new FilterSettings())
            .Add(FilterProperty.Map<Department, int>("Id", department => department.Id, Int32Filter.Profile).Build())
            .Add(FilterProperty.Map<Department, string>("Name", department => department.Name, StringFilter.Profile).Build())
            .Build();
        var definition = new FilterDefinition<Person>(new FilterSchemaBuilder<Person>(new FilterSettings())
            .AddRange(departmentSchema.LiftInto<Person>(person => person.Department, "Department", only: null, except: ["Name"]))
            .Build());

        // Act
        var excludedResult = definition.Validate(Leaf("department.name", "eq", "\"Ops\""));
        var keptResult = definition.Validate(Leaf("department.id", "eq", "1"));

        // Assert
        excludedResult.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.UnknownField);
        keptResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LiftInto_EmptyOnly_LiftsNoDirectProperties()
    {
        // Arrange
        var departmentSchema = DepartmentSchemaWithLiftedCompany();

        // Act
        var liftedProperties = departmentSchema.LiftInto<Person>(person => person.Department, "Department", only: []);

        // Assert
        liftedProperties.Select(property => property.Field).Should().Equal("Department.Company.Country");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void LiftInto_BlankPrefix_ThrowsConfigurationException(string prefix)
    {
        // Arrange
        var departmentSchema = DepartmentSchemaWithLiftedCompany();

        // Act
        var liftWithBlankPrefix = () => departmentSchema.LiftInto<Person>(person => person.Department, prefix);

        // Assert
        liftWithBlankPrefix.Should().Throw<FilterConfigurationException>().WithMessage("*non-empty prefix*");
    }

    [Fact]
    public void LiftInto_BlankPrefixOnAnEmptySchema_StillThrowsConfigurationException()
    {
        // Arrange
        var emptySchema = new FilterSchemaBuilder<Department>(new FilterSettings()).Build();

        // Act
        var liftWithBlankPrefix = () => emptySchema.LiftInto<Person>(person => person.Department, "  ");

        // Assert
        liftWithBlankPrefix.Should().Throw<FilterConfigurationException>().WithMessage("*non-empty prefix*");
    }

    [Fact]
    public void LiftInto_OnlyNamesUnknownProperty_ThrowsConfigurationException()
    {
        // Arrange
        var departmentSchema = DepartmentSchemaWithLiftedCompany();

        // Act
        var liftWithTypo = () => departmentSchema.LiftInto<Person>(person => person.Department, "Department", only: ["Naem"]);

        // Assert
        liftWithTypo.Should().Throw<FilterConfigurationException>().WithMessage("*'Naem'*Department*");
    }

    [Fact]
    public void LiftInto_ExceptNamesUnknownProperty_ThrowsConfigurationException()
    {
        // Arrange
        var departmentSchema = DepartmentSchemaWithLiftedCompany();

        // Act
        var liftWithTypo = () => departmentSchema.LiftInto<Person>(person => person.Department, "Department", except: ["Naem"]);

        // Assert
        liftWithTypo.Should().Throw<FilterConfigurationException>().WithMessage("*'Naem'*Department*");
    }

    [Fact]
    public void LiftInto_OnlyNamesTransitivePath_IsAcceptedBecauseABoundedNestingMayCutIt()
    {
        // Arrange
        var departmentSchema = DepartmentSchemaWithLiftedCompany();

        // Act
        var liftedProperties = departmentSchema.LiftInto<Person>(
            person => person.Department, "Department", only: ["Company.Headcount"]);

        // Assert
        liftedProperties.Select(property => property.Field).Should().Equal("Department.Company.Country");
    }

    [Fact]
    public void LiftInto_MultiHopNavigation_UsesTheWholeMemberPathAsField()
    {
        // Arrange
        var companySchema = new FilterSchemaBuilder<Company>(new FilterSettings())
            .Add(FilterProperty.Map<Company, string>("Country", company => company.Country, StringFilter.Profile).Build())
            .Build();

        // Act
        var liftedProperties = companySchema.LiftInto<Person>(person => person.Department.Company, "HomeCompany");

        // Assert
        var liftedProperty = liftedProperties.Should().ContainSingle().Subject;
        liftedProperty.Field.Should().Be("Department.Company.Country");
        liftedProperty.Alias.Should().Be("HomeCompany.Country");
    }

    [Fact]
    public void LiftInto_InterceptorOnSource_StillRunsOnTheHost()
    {
        // Arrange
        var departmentSchema = new FilterSchemaBuilder<Department>(new FilterSettings())
            .Add(FilterProperty.Map<Department, string>("Name", department => department.Name, StringFilter.Profile)
                .Intercept((_, value) => value.Trim())
                .Build())
            .Build();
        var definition = new FilterDefinition<Person>(new FilterSchemaBuilder<Person>(new FilterSettings())
            .AddRange(departmentSchema.LiftInto<Person>(person => person.Department, "Department"))
            .Build());

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("Department.Name", "eq", "\" Ops \"")).Names();

        // Assert
        filteredNames.Should().Equal("Bob");
    }

    [Fact]
    public void SchemaBuild_DuplicateWireKey_ThrowsConfigurationException()
    {
        // Act
        var buildWithDuplicate = () => Definition(properties:
        [
            FilterProperty.Map<Person, string>("Name", person => person.Name, StringFilter.Profile).Build(),
            FilterProperty.Map<Person, int>("Age", person => person.Age, Int32Filter.Profile).Alias("NAME").Build(),
        ]);

        // Assert
        buildWithDuplicate.Should().Throw<FilterConfigurationException>().WithMessage("*'NAME'*");
    }

    [Fact]
    public void SchemaBuild_TypedValueOperatorWithoutSerializerOptions_ThrowsConfigurationException()
    {
        // Arrange
        var typedValueProfile = Int32Filter.Profile.Extend("TypedInt",
            FilterOperator.Value<int, AgeRange>("within", (column, range) => column >= range.From && column <= range.To));

        // Act
        var buildWithoutOptions = () => Definition(properties: [FilterProperty.Map<Person, int>("Age", person => person.Age, typedValueProfile).Build()]);

        // Assert
        buildWithoutOptions.Should().Throw<FilterConfigurationException>().WithMessage("*JsonSerializerOptions*");
    }

    [Fact]
    public void ApplyFilter_TypedValueOperator_DeserializesThroughResolver()
    {
        // Arrange
        var typedValueProfile = Int32Filter.Profile.Extend("TypedInt",
            FilterOperator.Value<int, AgeRange>("within", (column, range) => column >= range.From && column <= range.To));
        var serializerOptions = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        var definition = Definition(
            serializerOptions: serializerOptions,
            properties: [FilterProperty.Map<Person, int>("Age", person => person.Age, typedValueProfile).Build()]);
        var validLeaf = Leaf("Age", "within", "{\"From\": 26, \"To\": 35}");

        // Act
        var validationResult = definition.Validate(validLeaf);
        var malformedResult = definition.Validate(Leaf("Age", "within", "\"nope\""));
        var filteredNames = definition.ApplyFilter(People(), validLeaf).Names();

        // Assert
        validationResult.IsValid.Should().BeTrue();
        malformedResult.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.InvalidValueType);
        filteredNames.Should().Equal("Alice");
    }

    [Fact]
    public void Validate_TypedValueOperatorWithJsonNullValue_ReturnsInvalidValueTypeError()
    {
        // Arrange
        var typedValueProfile = Int32Filter.Profile.Extend("TypedInt",
            FilterOperator.Value<int, AgeRange>("within", (column, range) => column >= range.From && column <= range.To));
        var definition = Definition(
            serializerOptions: new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() },
            properties: [FilterProperty.Map<Person, int>("Age", person => person.Age, typedValueProfile).Build()]);

        // Act
        var validationResult = definition.Validate(Leaf("Age", "within", "null"));

        // Assert
        validationResult.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.InvalidValueType);
    }

    [Fact]
    public void Validate_TypedArrayOperatorOnNullableColumnWithJsonNullValue_ReturnsInvalidValueTypeError()
    {
        // Arrange
        var typedArrayProfile = Int32Filter.Profile.Extend("TypedIntArray",
            FilterOperator.Value<int, int[]>("anyOf", (column, values) => values.Contains(column)));
        var definition = Definition(
            serializerOptions: new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() },
            properties: [FilterProperty.MapNullable<Person, int>("Score", person => person.Score, typedArrayProfile).Build()]);

        // Act
        var validationResult = definition.Validate(Leaf("Score", "anyOf", "null"));

        // Assert
        validationResult.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.InvalidValueType);
    }

    [Fact]
    public void SchemaBuild_TypedValueOperatorWithoutSerializerOptions_NamesTheOffendingOperator()
    {
        // Arrange
        var typedValueProfile = Int32Filter.Profile.Extend("TypedInt",
            FilterOperator.Value<int, AgeRange>("within", (column, range) => column >= range.From && column <= range.To));

        // Act
        var buildWithoutOptions = () => Definition(properties: [FilterProperty.Map<Person, int>("Age", person => person.Age, typedValueProfile).Build()]);

        // Assert
        buildWithoutOptions.Should().Throw<FilterConfigurationException>().WithMessage("*'within'*");
    }

    private static FilterSchema<Department> DepartmentSchemaWithLiftedCompany()
    {
        var companySchema = new FilterSchemaBuilder<Company>(new FilterSettings())
            .Add(FilterProperty.Map<Company, string>("Country", company => company.Country, StringFilter.Profile).Sortable().Build())
            .Build();
        return new FilterSchemaBuilder<Department>(new FilterSettings())
            .Add(FilterProperty.Map<Department, int>("Id", department => department.Id, Int32Filter.Profile).Sortable().Build())
            .Add(FilterProperty.Map<Department, string>("Name", department => department.Name, StringFilter.Profile).Build())
            .AddRange(companySchema.LiftInto<Department>(department => department.Company, "Company"))
            .Build();
    }

    public sealed record AgeRange(int From, int To);
}
