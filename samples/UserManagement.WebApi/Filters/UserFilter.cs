using Filtering.Net;
using UserManagement.WebApi.Models;

namespace UserManagement.WebApi.Filters;

// Feature catalogue: each grouping below demonstrates one Filtering.Net capability.
// String maps pass Profile = typeof(...) explicitly because StringFilterPlus makes string ambiguous (FN0014).
[GenerateFilter<User>]
public partial class UserFilter
{
    // Built-in primitive profiles (Int32, Bool, DateTime, Guid).
    [Map(nameof(User.Id), Sortable = true)]
    private static partial void MapId();

    // DefaultSortDirection.Desc — "sort by Age" lands newest-first.
    [Map(nameof(User.Age), Sortable = true, DefaultSortDirection = SortDir.Desc)]
    private static partial void MapAge();

    [Map(nameof(User.IsActive))]
    private static partial void MapIsActive();

    [Map(nameof(User.CreatedAt), Sortable = true, DefaultSortDirection = SortDir.Desc)]
    private static partial void MapCreatedAt();

    [Map(nameof(User.ExternalId))]
    private static partial void MapExternalId();

    [Map(nameof(User.DepartmentId), Sortable = true)]
    private static partial void MapDepartmentId();

    // Custom profile + typed-value operator: StringFilterPlus adds 'fuzzy' and 'ilike'. Forces the
    // generator to emit the IJsonTypeInfoResolver-accepting ctor on this class.
    [Map(nameof(User.Name), Profile = typeof(StringFilterPlus), Sortable = true)]
    private static partial void MapName();

    // Operator allow-list via Only.
    [Map(nameof(User.Email), Profile = typeof(StringFilter), Sortable = true,
        Only = new[] { "eq", "contains", "isNull" })]
    private static partial void MapEmail();

    // [InterceptValue] — runs once per leaf value before predicate building. Must be internal/public.
    [InterceptValue(nameof(User.Email))]
    internal static string NormalizeEmail(InterceptContext context, string value) => value.ToLowerInvariant();

    // Auto-emitted enum profile — generator emits Filtering.Net.Generated.UserStatusFilter.
    [Map(nameof(User.Status), Sortable = true)]
    private static partial void MapStatus();

    // Navigation path with Alias — exposes Department.Name as 'departmentName'.
    [Map("Department.Name", Profile = typeof(StringFilter), Alias = "departmentName", Sortable = true)]
    private static partial void MapDepartmentName();
}
