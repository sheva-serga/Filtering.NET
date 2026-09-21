using AwesomeAssertions;

using Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.MapNested;

/// <summary>
/// The <c>[MapNested]</c> joined predicates and joined <c>ORDER BY</c> against the two container
/// providers. Npgsql quotes and folds identifiers differently from SQLite, and SQL Server needs an
/// <c>ORDER BY</c> before it accepts <c>OFFSET</c>/<c>FETCH</c>, so a spliced path that translates
/// on SQLite can still fail here. Skipped automatically when Docker is unavailable.
/// </summary>
[Collection(nameof(MapNestedPostgresCollection))]
public class MapNestedPostgresTranslationTests(MapNestedPostgresFixture postgresFixture)
{
    private readonly MapNestedPostgresFixture _postgresFixture = postgresFixture;

    [Fact]
    public async Task ApplySorting_DepartmentNameAsc_ProducesOrderByOnJoinedColumnOnPostgres()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_postgresFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _postgresFixture.CreateResetContextAsync();
        await MapNestedSeed.SeedThreeUsersInUnsortedOrderAsync(dbContext, cancellationToken);
        var userFilter = new UserFilter();
        var sortItems = new List<SortItem> { new("department.name", SortDir.Asc) };

        // Act
        var sortedQuery = userFilter.ApplySorting(dbContext.Users.AsQueryable(), sortItems);
        var renderedSql = sortedQuery.ToQueryString();
        var orderedLogins = await sortedQuery.Select(user => user.Login).ToListAsync(cancellationToken);

        // Assert
        renderedSql.Should().Contain("ORDER BY");
        renderedSql.Should().Contain("JOIN");
        orderedLogins.Should().Equal(["alice", "bob", "carol"]);
    }

    [Fact]
    public async Task ApplyPagedAsync_DepartmentNameEq_ReturnsPageResultWithCorrectTotalOnPostgres()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_postgresFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _postgresFixture.CreateResetContextAsync();
        await MapNestedSeed.SeedFourUsersTwoMatchingAsync(dbContext, cancellationToken);
        var userFilter = new UserFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("department.name", "eq", "Sales"),
            Sort = [new SortItem("Id", SortDir.Asc)],
            Page = 1,
            PageSize = 1,
        };

        // Act
        var pageResult = await dbContext.Users.AsQueryable().ApplyPagedAsync(userFilter, request, cancellationToken);

        // Assert
        pageResult.TotalCount.Should().Be(2);
        pageResult.PageSize.Should().Be(1);
        pageResult.TotalPages.Should().Be(2);
        pageResult.Items.Select(user => user.Login).Should().Equal(["alice"]);
    }

    [Fact]
    public async Task ApplyFilter_ManagerNameEq_ProducesLeftJoinForTheOptionalNavigationOnPostgres()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_postgresFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _postgresFixture.CreateResetContextAsync();
        await MapNestedSeed.SeedManagerChainAsync(dbContext, cancellationToken);
        var employeeFilter = new EmployeeFilter();

        // Act
        var filteredQuery = employeeFilter.ApplyFilter(
            dbContext.Employees.AsQueryable(),
            FilterRequestBuilder.Leaf("manager.name", "eq", "Dana"));
        var renderedSql = filteredQuery.ToQueryString();
        var matchedNames = await filteredQuery.Select(employee => employee.Name).ToListAsync(cancellationToken);

        // Assert
        renderedSql.Should().Contain("LEFT JOIN", because: "Employee.Manager is optional, so rows without a manager must survive the join");
        matchedNames.Should().BeEquivalentTo(["Mia", "Noa"]);
    }
}

/// <summary>SQL Server half of the <c>[MapNested]</c> provider matrix.</summary>
[Collection(nameof(MapNestedSqlServerCollection))]
public class MapNestedSqlServerTranslationTests(MapNestedSqlServerFixture sqlServerFixture)
{
    private readonly MapNestedSqlServerFixture _sqlServerFixture = sqlServerFixture;

    [Fact]
    public async Task ApplySorting_DepartmentNameAsc_ProducesOrderByOnJoinedColumnOnSqlServer()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_sqlServerFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _sqlServerFixture.CreateResetContextAsync();
        await MapNestedSeed.SeedThreeUsersInUnsortedOrderAsync(dbContext, cancellationToken);
        var userFilter = new UserFilter();
        var sortItems = new List<SortItem> { new("department.name", SortDir.Asc) };

        // Act
        var sortedQuery = userFilter.ApplySorting(dbContext.Users.AsQueryable(), sortItems);
        var renderedSql = sortedQuery.ToQueryString();
        var orderedLogins = await sortedQuery.Select(user => user.Login).ToListAsync(cancellationToken);

        // Assert
        renderedSql.Should().Contain("ORDER BY");
        renderedSql.Should().Contain("JOIN");
        orderedLogins.Should().Equal(["alice", "bob", "carol"]);
    }

    [Fact]
    public async Task ApplyPagedAsync_DepartmentNameEqWithSort_EmitsOffsetFetchOnSqlServer()
    {
        // Arrange — paging a joined nested path is where SQL Server's "OFFSET requires ORDER BY"
        // rule bites, so this is the case SQLite cannot cover.
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_sqlServerFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _sqlServerFixture.CreateResetContextAsync();
        await MapNestedSeed.SeedFourUsersTwoMatchingAsync(dbContext, cancellationToken);
        var userFilter = new UserFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("department.name", "eq", "Sales"),
            Sort = [new SortItem("department.name", SortDir.Asc), new SortItem("Id", SortDir.Asc)],
            Page = 2,
            PageSize = 1,
        };

        // Act
        var pageResult = await dbContext.Users.AsQueryable().ApplyPagedAsync(userFilter, request, cancellationToken);

        // Assert
        pageResult.TotalCount.Should().Be(2);
        pageResult.PageSize.Should().Be(1);
        pageResult.Items.Select(user => user.Login).Should().Equal(["carol"]);
    }
}

/// <summary>Seed graphs shared by the SQLite and container-backed <c>[MapNested]</c> tests.</summary>
internal static class MapNestedSeed
{
    public static async Task SeedTwoUsersWithDifferentDepartmentsAsync(
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

    public static async Task SeedTwoLevelGraphAsync(
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

    public static async Task SeedThreeUsersInUnsortedOrderAsync(
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

    public static async Task SeedFourUsersTwoMatchingAsync(
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

    /// <summary>Dana manages Mia and Noa; Dana herself has no manager, so the optional navigation is null for one row.</summary>
    public static async Task SeedManagerChainAsync(
        MapNestedDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var director = new Employee { Id = 1, Name = "Dana" };
        dbContext.Employees.AddRange(
            director,
            new Employee { Id = 2, Name = "Mia", Manager = director },
            new Employee { Id = 3, Name = "Noa", Manager = director });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
