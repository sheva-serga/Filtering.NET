using System.Linq.Expressions;

namespace Filtering.Net;

/// <summary>Output of a <c>[PropertyMap]</c> override method. The source generator parses the override method body at compile time; this type primarily exists so user code compiles.</summary>
/// <typeparam name="TEntity">The entity type the rule targets.</typeparam>
/// <typeparam name="TValue">The value type of the property exposed by the rule.</typeparam>
/// <param name="PropertyAccessor">Expression selecting the property on the entity.</param>
/// <param name="Operators">Operator-name-to-predicate map describing the supported operators on the property.</param>
public sealed record FilterRule<TEntity, TValue>(
    Expression<Func<TEntity, TValue>> PropertyAccessor,
    IReadOnlyDictionary<string, Expression<Func<TValue, object?, bool>>> Operators);
