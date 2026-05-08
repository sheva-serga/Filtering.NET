using System.Text.Json;

using Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.MapNested;

/// <summary>SQL-translation proofs for <c>[MapNested]</c> dotted paths: spliced predicates must produce joined, parameterised SQL.</summary>
[Collection(nameof(MapNestedSqliteCollection))]
public class MapNestedSqlTranslationTests(MapNestedSqliteFixture sqliteFixture)
{
    private readonly MapNestedSqliteFixture _sqliteFixture = sqliteFixture;

    [Fact]
    public async Task ApplyFilter_DepartmentNameEq_ProducesLeftJoinAndParameterizedPredicate()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await SeedTwoUsersWithDifferentDepartmentsAsync(dbContext, cancellationToken);
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
        renderedSql.Should().Contain("JOIN", because: "the navigation department must materialise as a SQL join");
        var hasParameterPlaceholder = System.Text.RegularExpressions.Regex.IsMatch(renderedSql, "@\\w+");
        var hasParameterDeclaration = renderedSql.Contains(".param set ", StringComparison.Ordinal);
        (hasParameterPlaceholder || hasParameterDeclaration).Should().BeTrue(
            because: "the user-supplied 'Sales' literal must travel as a parameter, not an inline SQL literal");
    }

    [Fact]
    public async Task ApplyFilter_DepartmentCompanyCountryEq_ProducesTwoJoins()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await SeedTwoLevelGraphAsync(dbContext, cancellationToken);
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
        await SeedThreeUsersInUnsortedOrderAsync(dbContext, cancellationToken);
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
        await SeedFourUsersTwoMatchingAsync(dbContext, cancellationToken);
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

    private static async Task SeedTwoUsersWithDifferentDepartmentsAsync(
        MapNestedDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var salesCompany = new Company { Id = 1, Country = "DE" };
        var engineeringCompany = new Company { Id = 2, Country = "US" };
        dbContext.Companies.AddRange(salesCompany, engineeringCompany);
        var salesDepartment = new Department { Id = 1, Name = "Sales", Company = salesCompany };
        var engineeringDepartment = new Department { Id = 2, Name = "Engineering", Company = engineeringCompany };
        dbContext.Departments.AddRange(salesDepartment, engineeringDepartment);
        dbContext.Users.AddRange(
            new User { Id = 1, Login = "alice", Department = salesDepartment },
            new User { Id = 2, Login = "bob", Department = engineeringDepartment });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedTwoLevelGraphAsync(
        MapNestedDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var deCompany = new Company { Id = 1, Country = "DE" };
        var usCompany = new Company { Id = 2, Country = "US" };
        dbContext.Companies.AddRange(deCompany, usCompany);
        var berlinDepartment = new Department { Id = 1, Name = "Sales", Company = deCompany };
        var newYorkDepartment = new Department { Id = 2, Name = "Engineering", Company = usCompany };
        var munichDepartment = new Department { Id = 3, Name = "Sales", Company = deCompany };
        dbContext.Departments.AddRange(berlinDepartment, newYorkDepartment, munichDepartment);
        dbContext.Users.AddRange(
            new User { Id = 1, Login = "alice", Department = berlinDepartment },
            new User { Id = 2, Login = "bob", Department = newYorkDepartment },
            new User { Id = 3, Login = "carol", Department = munichDepartment });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedThreeUsersInUnsortedOrderAsync(
        MapNestedDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var company = new Company { Id = 1, Country = "DE" };
        dbContext.Companies.Add(company);
        var marketingDepartment = new Department { Id = 1, Name = "Marketing", Company = company };
        var engineeringDepartment = new Department { Id = 2, Name = "Engineering", Company = company };
        var salesDepartment = new Department { Id = 3, Name = "Sales", Company = company };
        dbContext.Departments.AddRange(marketingDepartment, engineeringDepartment, salesDepartment);
        dbContext.Users.AddRange(
            new User { Id = 1, Login = "carol", Department = salesDepartment },
            new User { Id = 2, Login = "alice", Department = engineeringDepartment },
            new User { Id = 3, Login = "bob", Department = marketingDepartment });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedFourUsersTwoMatchingAsync(
        MapNestedDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var company = new Company { Id = 1, Country = "DE" };
        dbContext.Companies.Add(company);
        var salesDepartment = new Department { Id = 1, Name = "Sales", Company = company };
        var engineeringDepartment = new Department { Id = 2, Name = "Engineering", Company = company };
        dbContext.Departments.AddRange(salesDepartment, engineeringDepartment);
        dbContext.Users.AddRange(
            new User { Id = 1, Login = "alice", Department = salesDepartment },
            new User { Id = 2, Login = "bob", Department = engineeringDepartment },
            new User { Id = 3, Login = "carol", Department = salesDepartment },
            new User { Id = 4, Login = "dave", Department = engineeringDepartment });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
