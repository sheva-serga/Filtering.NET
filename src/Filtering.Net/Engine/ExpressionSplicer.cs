using System.Linq.Expressions;

namespace Filtering.Net;

internal static class ExpressionSplicer
{
    // The replacement must have the parameter's type; nullable mismatches go through NullableColumnLifter.
    public static Expression ReplaceParameter(Expression body, ParameterExpression source, Expression replacement) =>
        new ParameterReplacer(source, replacement).Visit(body)!;

    public static Expression<Func<TParent, TValue>> Compose<TParent, TEntity, TValue>(
        Expression<Func<TParent, TEntity>> navigation,
        Expression<Func<TEntity, TValue>> accessor)
    {
        var composedBody = ReplaceParameter(accessor.Body, accessor.Parameters[0], navigation.Body);
        return Expression.Lambda<Func<TParent, TValue>>(composedBody, navigation.Parameters[0]);
    }

    private sealed class ParameterReplacer(ParameterExpression source, Expression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? replacement : base.VisitParameter(node);
    }
}
