using System.Text.Json;

using AwesomeAssertions;

using Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.MapNested;

/// <summary>SQL-translation proofs for <c>[MapNested]</c> dotted paths: spliced predicates must produce joined, parameterised SQL.</summary>
[Collection(nameof(MapNestedSqliteCollection))]
public class MapNestedSqlTranslationTests(MapNestedSqliteFixture sqliteFixture)
{
    private readonly MapNestedSqliteFixture _sqliteFixture = sqliteFixture;

    [Fact]
    public async Task ApplyFilter_DepartmentNameEq_ProducesInnerJoinAndParameterizedPredicate()
    {
        // Arrange — User.Department is a non-nullable reference navigation, so EF models it as
        // required and the spliced path must translate to an INNER JOIN.
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await MapNestedSeed.SeedTwoUsersWithDifferentDepartmentsAsync(dbContext, cancellationToken);
        var userFilter = new UserFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("department.name", "eq", "Sales"),
        };

        // Act
        var filteredQuery = userFilter.ApplyFilter(dbContext.Users.AsQueryable(), request.Where);
        var renderedSql = filteredQuery.ToQueryString();
        var matchedUsers = await filteredQuery.ToListAsync(cancellationToken);

        // Assert
        matchedUsers.Should().ContainSingle().Which.Login.Should().Be("alice");
        renderedSql.Should().Contain("WHERE");
        renderedSql.Should().Contain("INNER JOIN", because: "the required navigation department must materialise as an inner join");
        var hasParameterPlaceholder = System.Text.RegularExpressions.Regex.IsMatch(renderedSql, "@\\w+");
        var hasParameterDeclaration = renderedSql.Contains(".param set ", StringComparison.Ordinal);
        (hasParameterPlaceholder || hasParameterDeclaration).Should().BeTrue(
            because: "the user-supplied 'Sales' literal must travel as a parameter, not an inline SQL literal");
    }

    [Fact]
    public async Task ApplyFilter_ManagerNameEq_ProducesLeftJoinForTheOptionalNavigation()
    {
        // Arrange — Employee.Manager is optional (int? ManagerId), which is the shape that makes EF
        // emit a LEFT JOIN. Dana has no manager, so she must not match and must not break the join.
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await MapNestedSeed.SeedManagerChainAsync(dbContext, cancellationToken);
        var employeeFilter = new EmployeeFilter();

        // Act
        var filteredQuery = employeeFilter.ApplyFilter(
            dbContext.Employees.AsQueryable(),
            FilterRequestBuilder.Leaf("manager.name", "eq", "Dana"));
        var renderedSql = filteredQuery.ToQueryString();
        var matchedNames = await filteredQuery.Select(employee => employee.Name).ToListAsync(cancellationToken);

        // Assert
        renderedSql.Should().Contain("LEFT JOIN", because: "an optional reference navigation must not filter managerless rows out through the join itself");
        matchedNames.Should().BeEquivalentTo(["Mia", "Noa"]);
    }

    [Fact]
    public async Task ApplyFilter_DepartmentCompanyCountryEq_ProducesTwoJoins()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await MapNestedSeed.SeedTwoLevelGraphAsync(dbContext, cancellationToken);
        var userFilter = new UserFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("department.company.country", "eq", "DE"),
        };

        // Act
        var filteredQuery = userFilter.ApplyFilter(dbContext.Users.AsQueryable(), request.Where);
        var renderedSql = filteredQuery.ToQueryString();
        var matchedLogins = await filteredQuery.Select(user => user.Login).ToListAsync(cancellationToken);

        // Assert
        matchedLogins.Should().BeEquivalentTo(["alice", "carol"]);
        var joinCount = System.Text.RegularExpressions.Regex.Matches(renderedSql, "JOIN").Count;
        joinCount.Should().BeGreaterThanOrEqualTo(2,
            because: "the two-hop navigation department -> company must produce two SQL joins");
    }

    [Fact]
    public async Task ApplyFilter_SelfReferencingNestingTwoLevelsDeep_TranslatesToSelfJoins()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        var director = new Employee { Id = 1, Name = "Dana" };
        var manager = new Employee { Id = 2, Name = "Mia", Manager = director };
        dbContext.Employees.AddRange(
            director,
            manager,
            new Employee { Id = 3, Name = "Eli", Manager = manager },
            new Employee { Id = 4, Name = "Noa", Manager = director });
        await dbContext.SaveChangesAsync(cancellationToken);
        var employeeFilter = new EmployeeFilter();
        var grandManagerIsDana = FilterRequestBuilder.Leaf("manager.manager.name", "eq", "Dana");

        // Act
        var filteredQuery = employeeFilter.ApplyFilter(dbContext.Employees.AsQueryable(), grandManagerIsDana);
        var renderedSql = filteredQuery.ToQueryString();
        var matchedEmployees = await filteredQuery.ToListAsync(cancellationToken);
        var beyondDepth = employeeFilter.Validate(FilterRequestBuilder.Leaf("manager.manager.manager.name", "eq", "Dana"));

        // Assert
        matchedEmployees.Should().ContainSingle().Which.Name.Should().Be("Eli");
        System.Text.RegularExpressions.Regex.Matches(renderedSql, "JOIN").Count.Should().Be(2, because: "each manager hop is one self-join");
        beyondDepth.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.UnknownField);
    }

    [Fact]
    public void Validate_OnlyRestrictedSource_RejectsExcludedPath()
    {
        // Arrange
        var userFilterOnlyRestricted = new UserFilterOnlyRestricted();
        var excludedLeaf = new FilterLeaf(
            "department.name",
            "eq",
            JsonDocument.Parse("\"Sales\"").RootElement);

        // Act
        var validationResult = userFilterOnlyRestricted.Validate(excludedLeaf);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.UnknownField);
    }

    [Fact]
    public async Task ApplySorting_DepartmentNameAsc_ProducesOrderByOnJoinedColumn()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await MapNestedSeed.SeedThreeUsersInUnsortedOrderAsync(dbContext, cancellationToken);
        var userFilter = new UserFilter();
        var sortItems = new List<SortItem> { new("department.name", SortDir.Asc) };

        // Act
        var sortedQuery = userFilter.ApplySorting(dbContext.Users.AsQueryable(), sortItems);
        var renderedSql = sortedQuery.ToQueryString();
        var orderedLogins = await sortedQuery.Select(user => user.Login).ToListAsync(cancellationToken);

        // Assert
        renderedSql.Should().Contain("ORDER BY");
        renderedSql.Should().Contain("JOIN", because: "ordering by a nested-nav column must include the join");
        orderedLogins.Should().Equal(["alice", "bob", "carol"]);
    }

    [Fact]
    public async Task ApplyPagedAsync_DepartmentNameEq_ReturnsPageResultWithCorrectTotal()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await MapNestedSeed.SeedFourUsersTwoMatchingAsync(dbContext, cancellationToken);
        var userFilter = new UserFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("department.name", "eq", "Sales"),
        };

        // Act
        var pageResult = await dbContext.Users.AsQueryable()
            .ApplyPagedAsync(userFilter, request, cancellationToken);

        // Assert
        pageResult.TotalCount.Should().Be(2);
        pageResult.Items.Select(user => user.Login).Should().BeEquivalentTo(["alice", "carol"]);
    }
}
