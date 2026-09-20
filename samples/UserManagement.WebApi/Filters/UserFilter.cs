using Filtering.Net;

using UserManagement.WebApi.Models;

namespace UserManagement.WebApi.Filters;

// Feature catalogue: each grouping below demonstrates one Filtering.Net capability.
// String maps pass Profile = typeof(...) explicitly because StringFilterPlus makes string ambiguous (FN0014).
[GenerateFilter<User>]
// Built-in primitive profiles (Int32, Bool, DateTime, Guid).
[Map(nameof(User.Id), Sortable = true)]
// DefaultSortDirection.Desc — "sort by Age" lands newest-first.
[Map(nameof(User.Age), Sortable = true, DefaultSortDirection = SortDir.Desc)]
[Map(nameof(User.IsActive))]
[Map(nameof(User.CreatedAt), Sortable = true, DefaultSortDirection = SortDir.Desc)]
[Map(nameof(User.ExternalId))]
[Map(nameof(User.DepartmentId), Sortable = true)]
// Custom profile + typed-value operator: StringFilterPlus adds 'fuzzy' and 'ilike'. Their values go
// through the JSON resolver, so this class also gets the IJsonTypeInfoResolver-accepting ctor.
[Map(nameof(User.Name), Profile = typeof(StringFilterPlus), Sortable = true)]
// Operator allow-list via Only.
[Map(nameof(User.Email), Profile = typeof(StringFilter), Sortable = true, Only = new[] { "eq", "contains", "isNull" })]
// Auto-emitted enum profile — generator emits Filtering.Net.Generated.UserStatusFilter.
[Map(nameof(User.Status), Sortable = true)]
// [MapNested] — reuses DepartmentFilter's mappings under the 'department.' prefix.
[MapNested(nameof(User.Department))]
public partial class UserFilter
{
    // [InterceptValue] — runs once per leaf value before predicate building.
    [InterceptValue(nameof(User.Email))]
    private static string NormalizeEmail(InterceptContext context, string value) => value.ToLowerInvariant();

}
