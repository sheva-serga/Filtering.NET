using System.Linq.Expressions;

namespace Filtering.Net;

/// <summary>Combines typed predicate expressions while preserving a single parameter — required for EF Core translation.</summary>
public static class PredicateBuilder
{
    /// <summary>Combines two predicates with logical AND, rebinding the right-hand parameter to the left-hand one.</summary>
    public static Expression<Func<TEntity, bool>> AndAlso<TEntity>(
        this Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right)
        => Combine(left, right, Expression.AndAlso);

    /// <summary>Combines two predicates with logical OR, rebinding the right-hand parameter to the left-hand one.</summary>
    public static Expression<Func<TEntity, bool>> OrElse<TEntity>(
        this Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right)
        => Combine(left, right, Expression.OrElse);

    /// <summary>Negates a predicate while preserving its parameter identity.</summary>
    public static Expression<Func<TEntity, bool>> Not<TEntity>(
        this Expression<Func<TEntity, bool>> source)
        => Expression.Lambda<Func<TEntity, bool>>(Expression.Not(source.Body), source.Parameters[0]);

    private static Expression<Func<TEntity, bool>> Combine<TEntity>(
        Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right,
        Func<Expression, Expression, BinaryExpression> binaryFactory)
    {
        var rebindedRightBody = ExpressionSplicer.ReplaceParameter(right.Body, right.Parameters[0], left.Parameters[0]);
        var combinedBody = binaryFactory(left.Body, rebindedRightBody);
        return Expression.Lambda<Func<TEntity, bool>>(combinedBody, left.Parameters[0]);
    }
}
