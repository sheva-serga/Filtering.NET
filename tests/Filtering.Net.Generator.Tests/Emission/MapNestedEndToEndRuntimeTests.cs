using System.Text.Json;

using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>End-to-end runtime proof that <c>[MapNested]</c>'s spliced dispatch returns the right rows under <c>ApplyFilter</c> / <c>ApplySorting</c>.</summary>
public class MapNestedEndToEndRuntimeTests
{
    private const string TwoLevelSource = """
        using Filtering.Net;
        namespace Sample;
        public class Company { public string Country { get; set; } = ""; }
        public class Department
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public Company Company { get; set; } = new();
        }
        public class User
        {
            public string Login { get; set; } = "";
            public Department Department { get; set; } = new();
        }
        [Map(nameof(Company.Country))]
        [GenerateFilter<Company>] public partial class CompanyFilter
        {
        }
        [Map(nameof(Department.Id))]
        [Map(nameof(Department.Name), Sortable = true)]
        [MapNested(nameof(Department.Company))]
        [GenerateFilter<Department>] public partial class DepartmentFilter
        {
        }
        [GenerateFilter<User>]
        [Map(nameof(User.Login))]
        [MapNested(nameof(User.Department))]
        public partial class UserFilter
        {
        }
        """;

    private const string OnlyRestrictedSource = """
        using Filtering.Net;
        namespace Sample;
        public class Department
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
        public class User { public Department Department { get; set; } = new(); }
        [Map(nameof(Department.Id))]
        [Map(nameof(Department.Name))]
        [GenerateFilter<Department>] public partial class DepartmentFilter
        {
        }
        [GenerateFilter<User>]
        [MapNested(nameof(User.Department), Only = new[] { "Id" })]
        public partial class UserFilter
        {
        }
        """;

    private const string ExceptRestrictedSource = """
        using Filtering.Net;
        namespace Sample;
        public class Department
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string InternalNotes { get; set; } = "";
        }
        public class User { public Department Department { get; set; } = new(); }
        [Map(nameof(Department.Id))]
        [Map(nameof(Department.Name))]
        [Map(nameof(Department.InternalNotes))]
        [GenerateFilter<Department>] public partial class DepartmentFilter
        {
        }
        [GenerateFilter<User>]
        [MapNested(nameof(User.Department), Except = new[] { "InternalNotes" })]
        public partial class UserFilter
        {
        }
        """;

    [Fact]
    public void Filter_ExceptRestricted_RejectsTheExcludedPathAndKeepsTheRest()
    {
        // Arrange — Except is the only half of FilterSchema.IsPathAllowed that the snapshots cannot
        // observe: they pin the emitted argument, not the path set the schema ends up exposing.
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ExceptRestrictedSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var validateNodeMethod = userFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(FilterNode));

        // Act
        var excludedResult = (FilterValidationResult)validateNodeMethod.Invoke(instance,
            [new FilterLeaf("department.internalNotes", "eq", JsonDocument.Parse("\"secret\"").RootElement)])!;
        var keptResult = (FilterValidationResult)validateNodeMethod.Invoke(instance,
            [new FilterLeaf("department.name", "eq", JsonDocument.Parse("\"Sales\"").RootElement)])!;

        // Assert
        excludedResult.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.UnknownField);
        keptResult.IsValid.Should().BeTrue();
    }

    private const string NullableNavigationSource = """
        using Filtering.Net;
        namespace Sample;
        public class Department { public string Name { get; set; } = ""; }
        public class User { public string Login { get; set; } = ""; public Department? Department { get; set; } }
        [Map(nameof(Department.Name))]
        [GenerateFilter<Department>] public partial class DepartmentFilter
        {
        }
        [GenerateFilter<User>]
        [Map(nameof(User.Login))]
        [MapNested(nameof(User.Department))]
        public partial class UserFilter
        {
        }
        """;

    [Fact]
    public void Filter_NullNavigationInMemory_ThrowsBecauseLinqToObjectsDereferencesTheNavigation()
    {
        // Arrange — the spliced predicate reads entity.Department.Name with no null guard, which is
        // exactly what makes it a LEFT JOIN on a database provider. Over LINQ-to-Objects there is no
        // such translation, so a null navigation dereferences. Pinned so the difference between the
        // two execution models is deliberate rather than discovered in production.
        var assembly = RuntimeLoader.LoadGeneratedAssembly(NullableNavigationSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var departmentType = assembly.GetType("Sample.Department")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var userWithDepartment = CreateUserWithOptionalDepartment(userType, departmentType, "alice", "Sales");
        var userWithoutDepartment = CreateUserWithOptionalDepartment(userType, departmentType, "bob", departmentName: null);
        var typedQueryable = BuildTypedQueryable(userType, [userWithDepartment, userWithoutDepartment]);
        var leaf = new FilterLeaf("department.name", "eq", JsonDocument.Parse("\"Sales\"").RootElement);

        // Act
        var filteredQuery = userFilterType.GetMethod("ApplyFilter")!.Invoke(instance, [typedQueryable, (object?)leaf])!;
        var materialiseAllRows = () => MaterializeLogins(filteredQuery, userType);

        // Assert
        materialiseAllRows.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void Filter_NullNavigationOnRowsThatAllHaveOne_MatchesNormally()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(NullableNavigationSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var departmentType = assembly.GetType("Sample.Department")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var typedQueryable = BuildTypedQueryable(userType,
        [
            CreateUserWithOptionalDepartment(userType, departmentType, "alice", "Sales"),
            CreateUserWithOptionalDepartment(userType, departmentType, "bob", "Engineering"),
        ]);
        var leaf = new FilterLeaf("department.name", "eq", JsonDocument.Parse("\"Sales\"").RootElement);

        // Act
        var filteredQuery = userFilterType.GetMethod("ApplyFilter")!.Invoke(instance, [typedQueryable, (object?)leaf])!;
        var matchedLogins = MaterializeLogins(filteredQuery, userType);

        // Assert
        matchedLogins.Should().Equal(["alice"]);
    }

    private const string DisableSortingSource = """
        using Filtering.Net;
        namespace Sample;
        public class Department { public string Name { get; set; } = ""; }
        public class User { public Department Department { get; set; } = new(); }
        [Map(nameof(Department.Name), Sortable = true)]
        [GenerateFilter<Department>] public partial class DepartmentFilter
        {
        }
        [GenerateFilter<User>]
        [MapNested(nameof(User.Department), DisableSorting = true)]
        public partial class UserFilter
        {
        }
        """;

    private const string CustomProfileSource = """
        using System;
        using System.Linq.Expressions;
        using Filtering.Net;
        namespace Sample;
        [FilterProfile<string>(BasedOn = typeof(StringFilter))]
        public static class StringFilterPlus
        {
            [FilterOperator("fuzzy")]
            public static Expression<Func<string, string, bool>> Fuzzy =>
                (column, value) => column.Contains(value);
        }
        public class Department { public string Name { get; set; } = ""; }
        public class User { public Department Department { get; set; } = new(); }
        [Map(nameof(Department.Name), Profile = typeof(StringFilterPlus), Only = new[] { "fuzzy" })]
        [GenerateFilter<Department>] public partial class DepartmentFilter
        {
        }
        [GenerateFilter<User>]
        [MapNested(nameof(User.Department))]
        public partial class UserFilter
        {
        }
        """;

    private const string InterceptorSource = """
        using Filtering.Net;
        namespace Sample;
        public class Department { public string Email { get; set; } = ""; }
        public class User { public Department Department { get; set; } = new(); }
        [Map(nameof(Department.Email))]
        [GenerateFilter<Department>] public partial class DepartmentFilter
        {
            [InterceptValue(nameof(Department.Email))]
            internal static string LowercaseEmail(InterceptContext ctx, string value) => value.ToLowerInvariant();
        }
        [GenerateFilter<User>]
        [MapNested(nameof(User.Department))]
        public partial class UserFilter
        {
        }
        """;

    private const string NestedPropertyMapSource = """
        using Filtering.Net;
        namespace Sample;
        public class Department { public string Email { get; set; } = ""; }
        public class User { public Department Department { get; set; } = new(); }
        [GenerateFilter<Department>] public partial class DepartmentFilter
        {
            [PropertyMap("Domain")]
            private static FilterRule<Department, string> MapDomain(FilterRuleBuilder<Department, string> builder) =>
                builder.For(department => department.Email.Substring(department.Email.IndexOf('@') + 1))
                       .Operator<string>("eq", (domain, value) => domain == value);
        }
        [GenerateFilter<User>]
        [MapNested(nameof(User.Department))]
        public partial class UserFilter
        {
        }
        """;

    [Fact]
    public void Filter_SourcePropertyMapRule_RunsThroughNestedLiftWithHostResolver()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(NestedPropertyMapSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var departmentType = assembly.GetType("Sample.Department")!;
        var resolverConstructor = userFilterType.GetConstructor([typeof(System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver)])!;
        var instance = resolverConstructor.Invoke([new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()]);
        var users = new[]
        {
            CreateUserWithDepartmentEmail(userType, departmentType, "alice@corp.com"),
            CreateUserWithDepartmentEmail(userType, departmentType, "bob@example.org"),
        };
        var typedQueryable = BuildTypedQueryable(userType, users);
        var applyFilterMethod = userFilterType.GetMethod("ApplyFilter")!;
        var leaf = new FilterLeaf("department.domain", "eq", JsonDocument.Parse("\"corp.com\"").RootElement);

        // Act
        var filteredQuery = applyFilterMethod.Invoke(instance, [typedQueryable, (object?)leaf])!;
        var matchCount = 0;
        foreach (var _ in (System.Collections.IEnumerable)filteredQuery) matchCount++;

        // Assert
        matchCount.Should().Be(1);
    }

    private const string SelfReferencingSource = """
        using Filtering.Net;
        namespace Sample;
        public class Employee { public string Name { get; set; } = ""; public Employee? Manager { get; set; } }
        [GenerateFilter<Employee>]
        [Map(nameof(Employee.Name), Sortable = true)]
        [MapNested(nameof(Employee.Manager), MaxDepth = 2)]
        public partial class EmployeeFilter
        {
        }
        """;

    [Fact]
    public void Filter_SelfReferencingNestingWithMaxDepth_ExposesExactlyThatManyLevels()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(SelfReferencingSource);
        var employeeFilterType = assembly.GetType("Sample.EmployeeFilter")!;
        var employeeType = assembly.GetType("Sample.Employee")!;
        var instance = Activator.CreateInstance(employeeFilterType)!;
        object CreateEmployee(string name, object? manager)
        {
            var employee = Activator.CreateInstance(employeeType)!;
            employeeType.GetProperty("Name")!.SetValue(employee, name);
            employeeType.GetProperty("Manager")!.SetValue(employee, manager);
            return employee;
        }
        var employees = new[]
        {
            CreateEmployee("Eli", CreateEmployee("Mia", CreateEmployee("Dana", null))),
            CreateEmployee("Noa", CreateEmployee("Max", CreateEmployee("Zed", null))),
        };
        var typedQueryable = BuildTypedQueryable(employeeType, employees);
        var withinDepth = new FilterLeaf("manager.manager.name", "eq", JsonDocument.Parse("\"Dana\"").RootElement);
        var beyondDepth = new FilterLeaf("manager.manager.manager.name", "eq", JsonDocument.Parse("\"x\"").RootElement);
        var validateMethod = employeeFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(FilterNode));

        // Act
        var filteredQuery = employeeFilterType.GetMethod("ApplyFilter")!.Invoke(instance, [typedQueryable, (object?)withinDepth])!;
        var nameProperty = employeeType.GetProperty("Name")!;
        var matchedNames = ((System.Collections.IEnumerable)filteredQuery).Cast<object>()
            .Select(employee => (string)nameProperty.GetValue(employee)!).ToList();
        var beyondDepthResult = (FilterValidationResult)validateMethod.Invoke(instance, [beyondDepth])!;

        // Assert
        matchedNames.Should().Equal("Eli");
        beyondDepthResult.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.UnknownField);
    }

    [Fact]
    public void Filter_DepartmentName_Eq_ReturnsMatchingUsers()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(TwoLevelSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var departmentType = assembly.GetType("Sample.Department")!;
        var companyType = assembly.GetType("Sample.Company")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var users = new[]
        {
            CreateUser(userType, "alice", departmentType, 1, "Sales", companyType, "DE"),
            CreateUser(userType, "bob", departmentType, 2, "Engineering", companyType, "US"),
            CreateUser(userType, "carol", departmentType, 3, "Sales", companyType, "FR"),
        };
        var typedQueryable = BuildTypedQueryable(userType, users);
        var applyFilterMethod = userFilterType.GetMethod("ApplyFilter")!;
        // Wire path stays lower-case; CLR navigation is PascalCase.
        var leaf = new FilterLeaf("department.name", "eq", JsonDocument.Parse("\"Sales\"").RootElement);

        // Act
        var filteredQuery = applyFilterMethod.Invoke(instance, [typedQueryable, (object?)leaf])!;
        var matchedLogins = MaterializeLogins(filteredQuery, userType);

        // Assert
        matchedLogins.Should().BeEquivalentTo(["alice", "carol"]);
    }

    [Fact]
    public void Filter_DepartmentCompanyCountry_Eq_ReturnsMatchingUsers()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(TwoLevelSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var departmentType = assembly.GetType("Sample.Department")!;
        var companyType = assembly.GetType("Sample.Company")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var users = new[]
        {
            CreateUser(userType, "alice", departmentType, 1, "Sales", companyType, "DE"),
            CreateUser(userType, "bob", departmentType, 2, "Engineering", companyType, "US"),
            CreateUser(userType, "carol", departmentType, 3, "Sales", companyType, "DE"),
        };
        var typedQueryable = BuildTypedQueryable(userType, users);
        var applyFilterMethod = userFilterType.GetMethod("ApplyFilter")!;
        var leaf = new FilterLeaf("department.company.country", "eq", JsonDocument.Parse("\"DE\"").RootElement);

        // Act
        var filteredQuery = applyFilterMethod.Invoke(instance, [typedQueryable, (object?)leaf])!;
        var matchedLogins = MaterializeLogins(filteredQuery, userType);

        // Assert
        matchedLogins.Should().BeEquivalentTo(["alice", "carol"]);
    }

    [Fact]
    public void Filter_DeptNameFuzzyOperator_ExecutesViaInlinedLambdaBody()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(CustomProfileSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var departmentType = assembly.GetType("Sample.Department")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var users = new[]
        {
            CreateSimpleUser(userType, departmentType, "Sales"),
            CreateSimpleUser(userType, departmentType, "Engineering"),
            CreateSimpleUser(userType, departmentType, "Sales-EU"),
        };
        var typedQueryable = BuildTypedQueryable(userType, users);
        var applyFilterMethod = userFilterType.GetMethod("ApplyFilter")!;
        var leaf = new FilterLeaf("department.name", "fuzzy", JsonDocument.Parse("\"Sal\"").RootElement);

        // Act
        var filteredQuery = applyFilterMethod.Invoke(instance, [typedQueryable, (object?)leaf])!;
        var matchedDepartmentNames = MaterializeDepartmentNames(filteredQuery, userType, departmentType);

        // Assert
        matchedDepartmentNames.Should().BeEquivalentTo(["Sales", "Sales-EU"]);
    }

    [Fact]
    public void Filter_SourceInterceptor_RunsThroughNestedLift()
    {
        // The source filter's [InterceptValue] lower-cases the value, so upper-case "ALICE" matches
        // the lower-case stored email even though the leaf targets the host filter.
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(InterceptorSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var departmentType = assembly.GetType("Sample.Department")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var users = new[]
        {
            CreateUserWithDepartmentEmail(userType, departmentType, "alice@corp.com"),
            CreateUserWithDepartmentEmail(userType, departmentType, "bob@corp.com"),
        };
        var typedQueryable = BuildTypedQueryable(userType, users);
        var applyFilterMethod = userFilterType.GetMethod("ApplyFilter")!;
        var leaf = new FilterLeaf("department.email", "contains", JsonDocument.Parse("\"ALICE\"").RootElement);

        // Act
        var filteredQuery = applyFilterMethod.Invoke(instance, [typedQueryable, (object?)leaf])!;
        var matchCount = 0;
        foreach (var _ in (System.Collections.IEnumerable)filteredQuery) matchCount++;

        // Assert
        matchCount.Should().Be(1);
    }

    [Fact]
    public void Filter_OnlyRestricted_RejectsExcludedPath()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(OnlyRestrictedSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var excludedLeaf = new FilterLeaf("department.name", "eq", JsonDocument.Parse("\"Sales\"").RootElement);
        var validateNodeMethod = userFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(FilterNode));

        // Act
        var validationResult = (FilterValidationResult)validateNodeMethod.Invoke(instance, [excludedLeaf])!;

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().ContainSingle(e => e.Code == FilterValidationCode.UnknownField);
    }

    [Fact]
    public void Sort_DeptName_Asc_ReturnsOrdered()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(TwoLevelSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var departmentType = assembly.GetType("Sample.Department")!;
        var companyType = assembly.GetType("Sample.Company")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var users = new[]
        {
            CreateUser(userType, "carol", departmentType, 3, "Sales", companyType, "FR"),
            CreateUser(userType, "alice", departmentType, 1, "Engineering", companyType, "DE"),
            CreateUser(userType, "bob", departmentType, 2, "Marketing", companyType, "US"),
        };
        var typedQueryable = BuildTypedQueryable(userType, users);
        var sortItems = new List<SortItem> { new("department.name", SortDir.Asc) };
        var validateSortMethod = userFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(IReadOnlyList<SortItem>));
        var applySortingMethod = userFilterType.GetMethods()
            .First(m => m.Name == "ApplySorting" && m.GetParameters().Length == 4);

        // Act
        var validationResult = (FilterValidationResult)validateSortMethod.Invoke(instance, [sortItems])!;
        var sortedQuery = applySortingMethod.Invoke(instance, [typedQueryable, sortItems, (int?)null, (int?)null])!;
        var orderedLogins = MaterializeLogins(sortedQuery, userType);

        // Assert
        validationResult.IsValid.Should().BeTrue();
        orderedLogins.Should().Equal(["alice", "bob", "carol"]);
    }

    [Fact]
    public void Sort_DeptName_RejectedWhenDisableSortingTrue()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(DisableSortingSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var sortItems = new List<SortItem> { new("department.name", SortDir.Asc) };
        var validateSortMethod = userFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(IReadOnlyList<SortItem>));

        // Act
        var validationResult = (FilterValidationResult)validateSortMethod.Invoke(instance, [sortItems])!;

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().ContainSingle(e => e.Code == FilterValidationCode.NotSortable);
    }

    [Fact]
    public void Filter_DotPathThroughTwoNavs_TranslatesCorrectly()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(TwoLevelSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var departmentType = assembly.GetType("Sample.Department")!;
        var companyType = assembly.GetType("Sample.Company")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var users = new[]
        {
            CreateUser(userType, "alice", departmentType, 1, "Sales", companyType, "DE"),
            CreateUser(userType, "bob", departmentType, 1, "Sales", companyType, "US"),
            CreateUser(userType, "carol", departmentType, 2, "Sales", companyType, "DE"),
            CreateUser(userType, "dave", departmentType, 1, "Engineering", companyType, "DE"),
        };
        var typedQueryable = BuildTypedQueryable(userType, users);
        var applyFilterMethod = userFilterType.GetMethod("ApplyFilter")!;
        var combinedFilter = new FilterGroup(
            LogicalOp.And,
            [
                new FilterLeaf("department.id", "eq", JsonDocument.Parse("1").RootElement),
                new FilterLeaf("department.name", "eq", JsonDocument.Parse("\"Sales\"").RootElement),
                new FilterLeaf("department.company.country", "eq", JsonDocument.Parse("\"DE\"").RootElement),
            ]);

        // Act
        var filteredQuery = applyFilterMethod.Invoke(instance, [typedQueryable, (object?)combinedFilter])!;
        var matchedLogins = MaterializeLogins(filteredQuery, userType);

        // Assert
        matchedLogins.Should().ContainSingle().Which.Should().Be("alice");
    }

    private static object CreateUser(
        Type userType,
        string login,
        Type departmentType,
        int departmentId,
        string departmentName,
        Type companyType,
        string companyCountry)
    {
        var company = Activator.CreateInstance(companyType)!;
        companyType.GetProperty("Country")!.SetValue(company, companyCountry);
        var department = Activator.CreateInstance(departmentType)!;
        departmentType.GetProperty("Id")!.SetValue(department, departmentId);
        departmentType.GetProperty("Name")!.SetValue(department, departmentName);
        departmentType.GetProperty("Company")!.SetValue(department, company);
        var user = Activator.CreateInstance(userType)!;
        userType.GetProperty("Login")!.SetValue(user, login);
        userType.GetProperty("Department")!.SetValue(user, department);
        return user;
    }

    private static object CreateSimpleUser(Type userType, Type departmentType, string departmentName)
    {
        var department = Activator.CreateInstance(departmentType)!;
        departmentType.GetProperty("Name")!.SetValue(department, departmentName);
        var user = Activator.CreateInstance(userType)!;
        userType.GetProperty("Department")!.SetValue(user, department);
        return user;
    }

    private static object CreateUserWithOptionalDepartment(Type userType, Type departmentType, string login, string? departmentName)
    {
        var user = Activator.CreateInstance(userType)!;
        userType.GetProperty("Login")!.SetValue(user, login);
        if (departmentName is not null)
        {
            var department = Activator.CreateInstance(departmentType)!;
            departmentType.GetProperty("Name")!.SetValue(department, departmentName);
            userType.GetProperty("Department")!.SetValue(user, department);
        }
        return user;
    }

    private static object CreateUserWithDepartmentEmail(Type userType, Type departmentType, string email)
    {
        var department = Activator.CreateInstance(departmentType)!;
        departmentType.GetProperty("Email")!.SetValue(department, email);
        var user = Activator.CreateInstance(userType)!;
        userType.GetProperty("Department")!.SetValue(user, department);
        return user;
    }

    private static object BuildTypedQueryable(Type entityType, IEnumerable<object> entities)
    {
        var listType = typeof(List<>).MakeGenericType(entityType);
        var typedList = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;
        foreach (var entity in entities) addMethod.Invoke(typedList, [entity]);
        return typeof(Queryable).GetMethods()
            .First(m => m.Name == "AsQueryable" && m.IsGenericMethod)
            .MakeGenericMethod(entityType)
            .Invoke(null, [typedList])!;
    }

    private static List<string> MaterializeLogins(object query, Type userType)
    {
        var loginProperty = userType.GetProperty("Login")!;
        var results = new List<string>();
        foreach (var entity in (System.Collections.IEnumerable)query)
        {
            results.Add((string)loginProperty.GetValue(entity)!);
        }
        return results;
    }

    private static List<string> MaterializeDepartmentNames(object query, Type userType, Type departmentType)
    {
        var departmentProperty = userType.GetProperty("Department")!;
        var nameProperty = departmentType.GetProperty("Name")!;
        var results = new List<string>();
        foreach (var entity in (System.Collections.IEnumerable)query)
        {
            var department = departmentProperty.GetValue(entity)!;
            results.Add((string)nameProperty.GetValue(department)!);
        }
        return results;
    }

}
