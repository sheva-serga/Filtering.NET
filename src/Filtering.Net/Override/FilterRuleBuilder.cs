using System.Linq.Expressions;

namespace Filtering.Net;

/// <summary>Fluent builder used inside <c>[PropertyMap]</c> methods. The generated filter class runs the method once when it is constructed.</summary>
/// <typeparam name="TEntity">The entity type the rule targets.</typeparam>
/// <typeparam name="TValue">The type the accessor returns.</typeparam>
public sealed class FilterRuleBuilder<TEntity, TValue>
{
    private readonly List<FilterOperator<TValue>> _operators = [];
    private Expression<Func<TEntity, TValue>>? _propertyAccessor;

    /// <summary>Declares the accessor the rule's operators apply to.</summary>
    public FilterRuleBuilder<TEntity, TValue> For(Expression<Func<TEntity, TValue>> propertyAccessor)
    {
        _propertyAccessor = propertyAccessor ?? throw new ArgumentNullException(nameof(propertyAccessor));
        return this;
    }

    /// <summary>Declares an operator whose argument is deserialized through the definition's JSON type-info resolver.</summary>
    /// <typeparam name="TArgument">Type of the operator argument.</typeparam>
    public FilterRuleBuilder<TEntity, TValue> Operator<TArgument>(string operatorName, Expression<Func<TValue, TArgument, bool>> predicate)
    {
        AddOperator(FilterOperator.Value(operatorName, predicate));
        return this;
    }

    /// <summary>Declares an operator that takes no argument.</summary>
    public FilterRuleBuilder<TEntity, TValue> Operator(string operatorName, Expression<Func<TValue, bool>> predicate)
    {
        AddOperator(FilterOperator.Unary(operatorName, predicate));
        return this;
    }

    /// <summary>Completes the rule. Throws <see cref="FilterConfigurationException"/> when <see cref="For"/> was never called.</summary>
    public static implicit operator FilterRule<TEntity, TValue>(FilterRuleBuilder<TEntity, TValue> builder)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        var propertyAccessor = builder._propertyAccessor
            ?? throw new FilterConfigurationException("A [PropertyMap] rule must call For(...) before it is returned.");
        return new FilterRule<TEntity, TValue>(propertyAccessor, [.. builder._operators]);
    }

    private void AddOperator(FilterOperator<TValue> filterOperator)
    {
        foreach (var existingOperator in _operators)
        {
            if (string.Equals(existingOperator.Name, filterOperator.Name, StringComparison.OrdinalIgnoreCase))
                throw new FilterConfigurationException($"A [PropertyMap] rule declares operator '{filterOperator.Name}' more than once.");
        }
        _operators.Add(filterOperator);
    }
}
