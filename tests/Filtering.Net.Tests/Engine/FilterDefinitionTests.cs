using System.Linq.Expressions;

using AwesomeAssertions;

using Xunit;

using static Filtering.Net.Tests.Engine.EngineTestData;

namespace Filtering.Net.Tests.Engine;

public class FilterDefinitionTests
{
    [Fact]
    public void Validate_UnknownField_ReportsUnknownFieldAtLeafPath()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(Group(LogicalOp.And, Leaf("Name", "eq", "\"x\""), Leaf("Mystery", "eq", "1")));

        // Assert
        validationResult.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FilterValidationError(
                "where.and[1]", FilterValidationCode.UnknownField, "Field 'Mystery' is not configured for filtering.", Field: "Mystery"));
    }

    [Fact]
    public void Validate_OperatorNotOnProfile_ReportsOperatorNotAllowedAtOpPath()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(Leaf("Age", "contains", "1"));

        // Assert
        var validationError = validationResult.Errors.Should().ContainSingle().Subject;
        validationError.Path.Should().Be("where.op");
        validationError.Code.Should().Be(FilterValidationCode.OperatorNotAllowed);
        validationError.Message.Should().Be("Operator 'contains' is not supported on field 'Age'.");
    }

    [Fact]
    public void Validate_WrongValueKind_ReportsInvalidValueTypeAtValuePath()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(Leaf("age", "EQ", "true"));

        // Assert
        var validationError = validationResult.Errors.Should().ContainSingle().Subject;
        validationError.Path.Should().Be("where.value");
        validationError.Code.Should().Be(FilterValidationCode.InvalidValueType);
    }

    [Fact]
    public void Validate_UnaryOperatorWithValue_ReportsNoValueError()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(Leaf("Score", "isNull", "5"));

        // Assert
        validationResult.Errors.Should().ContainSingle()
            .Which.Message.Should().Be("Operator 'isNull' takes no value.");
    }

    [Fact]
    public void Validate_EmptyGroup_ReportsGroupEmpty()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(Group(LogicalOp.Or));

        // Assert
        validationResult.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.GroupEmpty && error.Path == "where");
    }

    [Fact]
    public void Validate_NestingBeyondLimit_ReportsNestingTooDeepOnce()
    {
        // Arrange
        var definition = StandardDefinition(new FilterSettings(MaxNestingDepth: 2));
        var tooDeep = Group(LogicalOp.And, Group(LogicalOp.Or, Leaf("Age", "eq", "1"), Leaf("Age", "eq", "2")));

        // Act
        var validationResult = definition.Validate(tooDeep);

        // Assert
        validationResult.Errors.Where(error => error.Code == FilterValidationCode.NestingTooDeep)
            .Select(error => error.Path).Should().Equal("where.and[0].or[0]", "where.and[0].or[1]");
    }

    [Fact]
    public void Validate_TooManyLeaves_ReportsTooManyConditionsAtRoot()
    {
        // Arrange
        var definition = StandardDefinition(new FilterSettings(MaxLeafConditions: 2));
        var threeLeaves = Group(LogicalOp.And, Leaf("Age", "eq", "1"), Leaf("Age", "eq", "2"), Leaf("Age", "eq", "3"));

        // Act
        var validationResult = definition.Validate(threeLeaves);

        // Assert
        validationResult.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.TooManyConditions && error.Path == "where");
    }

    [Fact]
    public void Validate_SortOnNonSortableField_ReportsNotSortable()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate([new SortItem("Name"), new SortItem("Score")]);

        // Assert
        validationResult.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FilterValidationError(
                "sort[1].field", FilterValidationCode.NotSortable, "Field 'Score' is not configured as sortable.", Field: "Score"));
    }

    [Fact]
    public void Validate_Request_AggregatesWhereSortAndPageErrors()
    {
        // Arrange
        var definition = StandardDefinition();
        var request = new FilterRequest
        {
            Where = Leaf("Mystery", "eq", "1"),
            Sort = [new SortItem("Mystery")],
            Page = 0,
            PageSize = 1000,
        };

        // Act
        var validationResult = definition.Validate(request);

        // Assert
        validationResult.Errors.Select(error => error.Code).Should().Equal(
            FilterValidationCode.UnknownField,
            FilterValidationCode.NotSortable,
            FilterValidationCode.PageInvalid,
            FilterValidationCode.PageSizeTooLarge);
    }

    [Fact]
    public void ApplyFilter_AndOrNotTree_FiltersRows()
    {
        // Arrange
        var definition = StandardDefinition();
        var where = Group(LogicalOp.And,
            Group(LogicalOp.Or, Leaf("Name", "startsWith", "\"A\""), Leaf("years", "gt", "40")),
            Group(LogicalOp.Not, Leaf("Name", "eq", "\"Carol\"")));

        // Act
        var filteredNames = definition.ApplyFilter(People(), where).Names();

        // Assert
        filteredNames.Should().Equal("Alice");
    }

    [Fact]
    public void ApplyFilter_InOperator_MatchesListedValues()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var filteredNames = definition.ApplyFilter(People(), Leaf("Age", "in", "[25, 41]")).Names();

        // Assert
        filteredNames.Should().Equal("Bob", "Carol");
    }

    [Fact]
    public void ApplyFilter_Value_IsCapturedAsMemberAccessSoProvidersParameterizeIt()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var filteredQuery = definition.ApplyFilter(People(), Leaf("Age", "eq", "30"));

        // Assert
        var inspector = new ValueShapeInspector();
        inspector.Visit(filteredQuery.Expression);
        inspector.ValueMemberAccessCount.Should().Be(1);
        inspector.BareIntConstantCount.Should().Be(0);
    }

    [Fact]
    public void ApplyFilter_UnvalidatedUnknownField_ThrowsDispatchException()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var applyUnknownField = () => definition.ApplyFilter(People(), Leaf("Mystery", "eq", "1"));

        // Assert
        applyUnknownField.Should().Throw<FilterDispatchException>();
    }

    [Fact]
    public void ApplySorting_TwoSortItems_OrdersByFirstThenSecond()
    {
        // Arrange
        var definition = Definition(
            properties:
            [
                FilterProperty.Map<Person, string>("Department.Name", person => person.Department.Name, StringFilter.Profile).Sortable().Build(),
                FilterProperty.Map<Person, int>("Age", person => person.Age, Int32Filter.Profile).Sortable().Build(),
            ]);

        // Act
        var sortedNames = definition.ApplySorting(People(), [new SortItem("department.name", SortDir.Desc), new SortItem("Age", SortDir.Desc)]).Names();

        // Assert
        sortedNames.Should().Equal("Carol", "Alice", "Bob");
    }

    [Fact]
    public void ApplySorting_OmittedDirection_UsesPropertyDefaultDirection()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var byDefaultDirection = definition.ApplySorting(People(), [new SortItem("Age")]).Names();
        var byExplicitDirection = definition.ApplySorting(People(), [new SortItem("Age", SortDir.Asc)]).Names();

        // Assert
        byDefaultDirection.Should().Equal("Carol", "Alice", "Bob");
        byExplicitDirection.Should().Equal("Bob", "Alice", "Carol");
    }

    [Fact]
    public void ApplySorting_PageAndPageSize_SkipsAndTakes()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var pagedNames = definition.ApplySorting(People(), [new SortItem("Name")], page: 2, pageSize: 2).Names();

        // Assert
        pagedNames.Should().Equal("Carol");
    }

    [Fact]
    public void ApplySorting_NoSortableProperties_ThrowsDispatchException()
    {
        // Arrange
        var definition = Definition(properties: [FilterProperty.Map<Person, int>("Age", person => person.Age, Int32Filter.Profile).Build()]);

        // Act
        var sortUnsortable = () => definition.ApplySorting(People(), [new SortItem("Age")]);

        // Assert
        sortUnsortable.Should().Throw<FilterDispatchException>().WithMessage("No sortable fields are configured (got 'Age').");
    }

    private sealed class ValueShapeInspector : ExpressionVisitor
    {
        public int ValueMemberAccessCount { get; private set; }

        public int BareIntConstantCount { get; private set; }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ConstantExpression && node.Member.Name == "Value")
            {
                ValueMemberAccessCount++;
                return node;
            }
            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is int) BareIntConstantCount++;
            return base.VisitConstant(node);
        }
    }
}
