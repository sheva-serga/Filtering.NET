using System.Linq.Expressions;

namespace Filtering.Net;

/// <summary>Output of a <c>[PropertyMap]</c> override method; exists so user code compiles while the source generator parses the body at compile time.</summary>
/// <typeparam name="TEntity">The entity type the rule targets.</typeparam>
/// <typeparam name="TValue">The value type of the property exposed by the rule.</typeparam>
public sealed record FilterRule<TEntity, TValue>(
    Expression<Func<TEntity, TValue>> PropertyAccessor,
    IReadOnlyDictionary<string, Expression<Func<TValue, object?, bool>>> Operators);
