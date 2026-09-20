namespace Filtering.Net.EntityFrameworkCore.Tests.MapNested;

[GenerateFilter<Employee>]
[Map(nameof(Employee.Name), Sortable = true)]
[MapNested(nameof(Employee.Manager), MaxDepth = 2)]
public partial class EmployeeFilter
{
}

[GenerateFilter<Company>]
[Map(nameof(Company.Country), Sortable = true)]
public partial class CompanyFilter
{
}

[GenerateFilter<Department>]
[Map(nameof(Department.Id), Sortable = true)]
[Map(nameof(Department.Name), Sortable = true)]
[MapNested(nameof(Department.Company))]
public partial class DepartmentFilter
{
}

[GenerateFilter<User>]
[Map(nameof(User.Id), Sortable = true)]
[Map(nameof(User.Login), Sortable = true)]
[MapNested(nameof(User.Department))]
public partial class UserFilter
{
}

[GenerateFilter<User>]
[Map(nameof(User.Id), Sortable = true)]
[MapNested(nameof(User.Department), Only = new[] { "Id" })]
public partial class UserFilterOnlyRestricted
{
}
