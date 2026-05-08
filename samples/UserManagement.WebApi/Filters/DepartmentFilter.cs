using Filtering.Net;
using UserManagement.WebApi.Models;

namespace UserManagement.WebApi.Filters;

[GenerateFilter<Department>]
public partial class DepartmentFilter
{
    [Map(nameof(Department.Id), Sortable = true)]
    private static partial void MapId();

    // Profile = typeof(StringFilter) is explicit because StringFilterPlus makes string ambiguous (FN0014).
    [Map(nameof(Department.Name), Profile = typeof(StringFilter), Sortable = true)]
    private static partial void MapName();
}
