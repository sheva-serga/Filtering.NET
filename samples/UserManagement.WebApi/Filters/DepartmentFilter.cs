using Filtering.Net;

using UserManagement.WebApi.Models;

namespace UserManagement.WebApi.Filters;

[GenerateFilter<Department>]
[Map(nameof(Department.Id), Sortable = true)]
// Profile = typeof(StringFilter) is explicit because StringFilterPlus makes string ambiguous (FN0014).
[Map(nameof(Department.Name), Profile = typeof(StringFilter), Sortable = true)]
public partial class DepartmentFilter;
