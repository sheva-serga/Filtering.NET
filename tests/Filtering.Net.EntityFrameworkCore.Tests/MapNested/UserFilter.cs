namespace Filtering.Net.EntityFrameworkCore.Tests.MapNested;

[GenerateFilter<Employee>]
public partial class EmployeeFilter
{
    [Map(nameof(Employee.Name), Sortable = true)]
    private static partial void MapName();

    [MapNested(nameof(Employee.Manager), MaxDepth = 2)]
    private static partial void MapManager();
}

[GenerateFilter<Company>]
public partial class CompanyFilter
{
    [Map(nameof(Company.Country), Sortable = true)]
    private static partial void MapCountry();
}

[GenerateFilter<Department>]
public partial class DepartmentFilter
{
    [Map(nameof(Department.Id), Sortable = true)]
    private static partial void MapId();

    [Map(nameof(Department.Name), Sortable = true)]
    private static partial void MapName();

    [MapNested(nameof(Department.Company))]
    private static partial void MapCompany();
}

[GenerateFilter<User>]
public partial class UserFilter
{
    [Map(nameof(User.Id), Sortable = true)]
    private static partial void MapId();

    [Map(nameof(User.Login), Sortable = true)]
    private static partial void MapLogin();

    [MapNested(nameof(User.Department))]
    private static partial void MapDept();
}

[GenerateFilter<User>]
public partial class UserFilterOnlyRestricted
{
    [Map(nameof(User.Id), Sortable = true)]
    private static partial void MapId();

    [MapNested(nameof(User.Department), Only = new[] { "Id" })]
    private static partial void MapDept();
}
