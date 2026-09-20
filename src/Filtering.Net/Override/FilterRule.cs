using System.Linq.Expressions;

namespace Filtering.Net;

/// <summary>Output of a <c>[PropertyMap]</c> method: a custom accessor and the operators that apply to it.</summary>
/// <typeparam name="TEntity">The entity type the rule targets.</typeparam>
/// <typeparam name="TValue">The type the accessor returns.</typeparam>
public sealed record FilterRule<TEntity, TValue>(
    Expression<Func<TEntity, TValue>> PropertyAccessor,
    IReadOnlyList<FilterOperator<TValue>> Operators);
