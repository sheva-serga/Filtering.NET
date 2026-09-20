using System.Linq.Expressions;

using Filtering.Net;

namespace UserManagement.WebApi.Filters;

// Custom string profile: inherits every operator from the built-in StringFilter via BasedOn and adds two more.
[FilterProfile<string>(BasedOn = typeof(StringFilter))]
public static class StringFilterPlus
{
    // Case-insensitive substring — String.Contains translates to a SQL LIKE on most providers.
    [FilterOperator("fuzzy")]
#pragma warning disable CA1862
    public static Expression<Func<string, string, bool>> Fuzzy =>
        (column, value) => column.Contains(value.ToLower());
#pragma warning restore CA1862

    // EF.Functions.* inside a [FilterOperator] body — translates to PostgreSQL ILIKE under Npgsql.
    [FilterOperator("ilike")]
    public static Expression<Func<string, string, bool>> ILike =>
        (column, pattern) => EF.Functions.ILike(column, pattern);
}
