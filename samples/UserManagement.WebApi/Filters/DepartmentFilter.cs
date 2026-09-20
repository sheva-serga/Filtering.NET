using Filtering.Net;

using UserManagement.WebApi.Models;

namespace UserManagement.WebApi.Filters;

[GenerateFilter<Department>]
[Map(nameof(Department.Id), Sortable = true)]
[Map(nameof(Department.Name), Profile = typeof(StringFilterPlus), Sortable = true)]
public partial class DepartmentFilter;
