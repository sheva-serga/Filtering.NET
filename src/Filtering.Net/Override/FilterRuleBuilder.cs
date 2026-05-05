using System.Linq.Expressions;

namespace Filtering.Net;

/// <summary>Fluent builder used inside <c>[PropertyMap]</c> override methods. The source generator parses calls to <see cref="For"/> and <see cref="Operator{TArgument}"/> at compile time. Calling these methods at runtime throws <see cref="FilterConfigurationException"/>.</summary>
/// <typeparam name="TEntity">The entity type the rule targets.</typeparam>
/// <typeparam name="TValue">The value type of the property the rule exposes.</typeparam>
public sealed class FilterRuleBuilder<TEntity, TValue>
{
    /// <summary>Declares the property accessor for the override. The source generator inlines this expression as the property accessor in generated leaf methods.</summary>
    /// <param name="propertyAccessor">Expression selecting the property on the entity.</param>
    /// <returns>The same builder for further chaining (parsed at compile time).</returns>
    /// <exception cref="FilterConfigurationException">Always thrown if invoked at runtime.</exception>
    public FilterRuleBuilder<TEntity, TValue> For(Expression<Func<TEntity, TValue>> propertyAccessor)
        => throw new FilterConfigurationException(
            "FilterRuleBuilder.For called at runtime - should be parsed by the source generator.");

    /// <summary>Declares one operator with a typed predicate. The source generator extracts the predicate body and inlines it.</summary>
    /// <typeparam name="TArgument">Type of the operator argument (the right-hand side of the predicate).</typeparam>
    /// <param name="operatorName">Operator name as it appears in filter requests.</param>
    /// <param name="predicate">Predicate expression describing the operator semantics.</param>
    /// <returns>The same builder for further chaining (parsed at compile time).</returns>
    /// <exception cref="FilterConfigurationException">Always thrown if invoked at runtime.</exception>
    public FilterRuleBuilder<TEntity, TValue> Operator<TArgument>(string operatorName, Expression<Func<TValue, TArgument, bool>> predicate)
        => throw new FilterConfigurationException(
            "FilterRuleBuilder.Operator called at runtime - should be parsed by the source generator.");

    /// <summary>Implicit conversion from builder to rule. Source generator supplies the rule materialization; runtime calls throw.</summary>
    /// <param name="builder">The builder to convert.</param>
    /// <exception cref="FilterConfigurationException">Always thrown if invoked at runtime.</exception>
    public static implicit operator FilterRule<TEntity, TValue>(FilterRuleBuilder<TEntity, TValue> builder)
        => throw new FilterConfigurationException(
            "FilterRuleBuilder coerced at runtime - should be parsed by the source generator.");
}
