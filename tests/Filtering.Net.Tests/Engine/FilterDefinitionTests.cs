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
    public void Validate_ArrayOperatorWithNonArrayValue_ReportsInvalidValueTypeAtValuePath()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(Leaf("Age", "in", "10"));

        // Assert — without this the request passes validation and blows up as a 500 in ApplyFilter.
        var validationError = validationResult.Errors.Should().ContainSingle().Subject;
        validationError.Path.Should().Be("where.value");
        validationError.Code.Should().Be(FilterValidationCode.InvalidValueType);
    }

    [Fact]
    public void Validate_ArrayOperatorWithJsonArray_ReportsNoError()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(Leaf("Age", "in", "[25, 41]"));

        // Assert
        validationResult.IsValid.Should().BeTrue();
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
    public void Validate_NotGroupWithTwoChildren_ReportsInvalidNodeShape()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(Group(LogicalOp.Not, Leaf("Age", "eq", "1"), Leaf("Age", "eq", "2")));

        // Assert
        var validationError = validationResult.Errors.Should().ContainSingle().Subject;
        validationError.Path.Should().Be("where");
        validationError.Code.Should().Be(FilterValidationCode.InvalidNodeShape);
        validationError.Message.Should().Be("A 'not' group requires exactly one child, got 2.");
    }

    [Fact]
    public void Validate_UndefinedCombinator_ReportsInvalidNodeShape()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(Group((LogicalOp)7, Leaf("Age", "eq", "1")));

        // Assert
        var validationError = validationResult.Errors.Should().ContainSingle().Subject;
        validationError.Path.Should().Be("where");
        validationError.Code.Should().Be(FilterValidationCode.InvalidNodeShape);
        validationError.Message.Should().Be("Group combinator '7' is not And, Or, or Not.");
    }

    [Fact]
    public void Validate_UnknownNodeSubtype_ReportsInvalidNodeShape()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(new UnsupportedNode());

        // Assert
        var validationError = validationResult.Errors.Should().ContainSingle().Subject;
        validationError.Path.Should().Be("where");
        validationError.Code.Should().Be(FilterValidationCode.InvalidNodeShape);
    }

    [Fact]
    public void Validate_LeafWithNullField_ReportsUnknownField()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(new FilterLeaf(null!, "eq", default));

        // Assert
        validationResult.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(FilterValidationCode.UnknownField);
    }

    [Fact]
    public void Validate_NestingBeyondLimit_ReportsNestingTooDeepPerOverDeepNode()
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
    public void Validate_SortItemWithoutField_ReportsNotSortable()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate([new SortItem(null!), new SortItem("")]);

        // Assert
        validationResult.Errors.Should().HaveCount(2);
        validationResult.Errors.Select(error => error.Path).Should().Equal("sort[0].field", "sort[1].field");
        validationResult.Errors.Should().AllSatisfy(error =>
        {
            error.Code.Should().Be(FilterValidationCode.NotSortable);
            error.Message.Should().Be("A sort item must name a field.");
        });
    }

    [Fact]
    public void Validate_SortDirectionOutsideEnum_ReportsInvalidSortDirection()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate([new SortItem("Name", (SortDir)7)]);

        // Assert
        validationResult.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FilterValidationError(
                "sort[0].dir", FilterValidationCode.InvalidSortDirection, "Sort direction '7' is not Asc or Desc.", Field: "Name"));
    }

    [Fact]
    public void Validate_PageOffsetBeyondIntRange_ReportsPageInvalid()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(page: int.MaxValue, pageSize: 200);

        // Assert
        var validationError = validationResult.Errors.Should().ContainSingle().Subject;
        validationError.Path.Should().Be("page");
        validationError.Code.Should().Be(FilterValidationCode.PageInvalid);
        validationError.Message.Should().Be("page 2147483647 is too large for a page size of 200.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageSizeBelowOne_ReportsPageSizeInvalid(int requestedPageSize)
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(page: 1, pageSize: requestedPageSize);

        // Assert — without the error the size is silently clamped to 1 and the client is told nothing.
        var validationError = validationResult.Errors.Should().ContainSingle().Subject;
        validationError.Path.Should().Be("pageSize");
        validationError.Code.Should().Be(FilterValidationCode.PageSizeInvalid);
        validationError.Message.Should().Be($"pageSize must be 1 or greater (was {requestedPageSize}).");
    }

    [Fact]
    public void Validate_LargePageWithRepresentableOffset_ReportsNoError()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var validationResult = definition.Validate(page: 10_000_000, pageSize: 200);

        // Assert
        validationResult.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, 50)]
    [InlineData(10, 10)]
    [InlineData(1000, 200)]
    [InlineData(0, 1)]
    public void ResolvePageSize_RequestedSize_ClampsIntoConfiguredRange(int? requestedPageSize, int expectedPageSize)
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var resolvedPageSize = definition.ResolvePageSize(requestedPageSize);

        // Assert
        resolvedPageSize.Should().Be(expectedPageSize);
    }

    [Fact]
    public void ResolvePageSize_CustomSettings_UsesConfiguredDefault()
    {
        // Arrange
        var definition = StandardDefinition(new FilterSettings(DefaultPageSize: 25, MaxPageSize: 75));

        // Act
        var resolvedPageSize = definition.ResolvePageSize(null);

        // Assert
        resolvedPageSize.Should().Be(25);
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
    public void ApplyFilter_UnvalidatedUnknownOperator_ThrowsDispatchException()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var applyUnknownOperator = () => definition.ApplyFilter(People(), Leaf("Age", "mystery", "1"));

        // Assert
        applyUnknownOperator.Should().Throw<FilterDispatchException>()
            .WithMessage("Unknown operator 'mystery' for 'Age' (validation should have caught this).");
    }

    [Fact]
    public void ApplyFilter_UnvalidatedUnparsableValue_ThrowsDispatchException()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act — a JSON string where Int32Filter.TryGetValue expects a number. Swallowing this would
        // silently filter on default(int) instead of failing.
        var applyUnparsableValue = () => definition.ApplyFilter(People(), Leaf("Age", "eq", "\"abc\""));

        // Assert
        applyUnparsableValue.Should().Throw<FilterDispatchException>()
            .WithMessage("Apply-time value extraction failed:*");
    }

    [Fact]
    public void ApplyFilter_UnvalidatedUnparsableArrayValue_ThrowsDispatchException()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var applyUnparsableArray = () => definition.ApplyFilter(People(), Leaf("Age", "in", "10"));

        // Assert
        applyUnparsableArray.Should().Throw<FilterDispatchException>()
            .WithMessage("Apply-time array extraction failed:*");
    }

    [Fact]
    public void ApplyFilter_UnvalidatedSingleChildGroupWithUndefinedCombinator_ThrowsDispatchException()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var applyUndefinedCombinator = () => definition.ApplyFilter(People(), Group((LogicalOp)7, Leaf("Age", "eq", "30")));

        // Assert
        applyUndefinedCombinator.Should().Throw<FilterDispatchException>().WithMessage("Unknown LogicalOp '7'*");
    }

    public static TheoryData<SortDir, string[]> SecondarySortCases => new()
    {
        { SortDir.Asc, ["Sam", "Sara", "Otto", "Olive"] },
        { SortDir.Desc, ["Sara", "Sam", "Olive", "Otto"] },
    };

    [Theory]
    [MemberData(nameof(SecondarySortCases))]
    public void ApplySorting_SecondarySortItem_AppliesThenByInThatDirection(SortDir secondaryDirection, string[] expectedNames)
    {
        // Arrange — two tied departments with two people each, so neither a lone primary sort (which
        // is stable and would keep source order inside a group) nor a lone secondary sort can produce
        // the expected sequence. Only a real ThenBy/ThenByDescending on top of the primary key does.
        var definition = Definition(
            properties:
            [
                FilterProperty.Map<Person, string>("Department.Name", person => person.Department.Name, StringFilter.Profile).Sortable().Build(),
                FilterProperty.Map<Person, int>("Age", person => person.Age, Int32Filter.Profile).Sortable().Build(),
            ]);

        // Act
        var sortedNames = definition
            .ApplySorting(PeopleInTwoTiedDepartments(), [new SortItem("department.name", SortDir.Desc), new SortItem("Age", secondaryDirection)])
            .Names();

        // Assert
        sortedNames.Should().Equal(expectedNames);
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
    public void ApplySorting_PageWithoutPageSize_TakesTheConfiguredDefaultPageSize()
    {
        // Arrange — DefaultPageSize and MaxPageSize differ so a fallback onto the wrong one shows up.
        var definition = StandardDefinition(new FilterSettings(DefaultPageSize: 2, MaxPageSize: 3));

        // Act
        var pagedNames = definition.ApplySorting(People(), [new SortItem("Name")], page: 1, pageSize: null).Names();

        // Assert
        pagedNames.Should().Equal("Alice", "Bob");
    }

    [Fact]
    public void ApplySorting_PageSizeAboveMaximum_ClampsToMaxPageSize()
    {
        // Arrange
        var definition = StandardDefinition(new FilterSettings(DefaultPageSize: 2, MaxPageSize: 3));

        // Act
        var pagedNames = definition.ApplySorting(People(), [new SortItem("Name")], page: 1, pageSize: 100).Names();

        // Assert
        pagedNames.Should().Equal("Alice", "Bob", "Carol");
    }

    [Fact]
    public void ApplySorting_PageBelowOne_ReturnsTheFirstPage()
    {
        // Arrange
        var definition = StandardDefinition(new FilterSettings(DefaultPageSize: 2, MaxPageSize: 3));

        // Act
        var pagedNames = definition.ApplySorting(People(), [new SortItem("Name")], page: 0, pageSize: 2).Names();

        // Assert
        pagedNames.Should().Equal("Alice", "Bob");
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

    [Fact]
    public void ApplySorting_MappedButNotSortableField_ThrowsDispatchExceptionNamingSortability()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var sortOnNonSortable = () => definition.ApplySorting(People(), [new SortItem("Score")]);

        // Assert
        sortOnNonSortable.Should().Throw<FilterDispatchException>()
            .WithMessage("Field 'Score' is not configured as sortable (validation should have caught this).");
    }

    [Fact]
    public void ApplySorting_UnvalidatedPageOffsetBeyondIntRange_ThrowsDispatchException()
    {
        // Arrange
        var definition = StandardDefinition();

        // Act
        var pageBeyondRange = () => definition.ApplySorting(People(), sortItems: null, page: int.MaxValue, pageSize: 200);

        // Assert
        pageBeyondRange.Should().Throw<FilterDispatchException>().WithMessage("*2147483647*200*");
    }

    private static IQueryable<Person> PeopleInTwoTiedDepartments() => new[]
    {
        new Person { Name = "Olive", Age = 30, Department = new Department { Name = "Ops" } },
        new Person { Name = "Sam", Age = 25, Department = new Department { Name = "Sales" } },
        new Person { Name = "Otto", Age = 20, Department = new Department { Name = "Ops" } },
        new Person { Name = "Sara", Age = 40, Department = new Department { Name = "Sales" } },
    }.AsQueryable();

    private sealed record UnsupportedNode : FilterNode;

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
