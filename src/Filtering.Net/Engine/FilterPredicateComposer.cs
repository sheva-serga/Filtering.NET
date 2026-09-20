using System.Linq.Expressions;

namespace Filtering.Net;

internal static class FilterPredicateComposer
{
    public static Expression<Func<TEntity, bool>> Compose<TEntity>(FilterSchema<TEntity> schema, FilterNode node) =>
        ComposeNode(schema, node, new FilterValueContext(schema.SerializerOptions));

    private static Expression<Func<TEntity, bool>> ComposeNode<TEntity>(FilterSchema<TEntity> schema, FilterNode node, FilterValueContext valueContext)
    {
        if (node is FilterGroup group) return ComposeGroup(schema, group, valueContext);
        if (node is FilterLeaf leaf)
        {
            if (!schema.TryGetProperty(leaf.Field, out var property))
                throw new FilterDispatchException($"Unknown field '{leaf.Field}' (validation should have caught this).");
            return property.BuildPredicate(leaf, valueContext);
        }
        throw new FilterDispatchException("Unknown FilterNode subtype.");
    }

    private static Expression<Func<TEntity, bool>> ComposeGroup<TEntity>(FilterSchema<TEntity> schema, FilterGroup group, FilterValueContext valueContext)
    {
        if (group.Children.Count == 0)
            throw new FilterDispatchException("Group has no children (validation should have caught this).");

        if (group.Op == LogicalOp.Not)
        {
            if (group.Children.Count != 1) throw new FilterDispatchException("Not group requires exactly one child.");
            return PredicateBuilder.Not(ComposeNode(schema, group.Children[0], valueContext));
        }

        var combinedPredicate = ComposeNode(schema, group.Children[0], valueContext);
        for (var childIndex = 1; childIndex < group.Children.Count; childIndex++)
        {
            var nextPredicate = ComposeNode(schema, group.Children[childIndex], valueContext);
            combinedPredicate = group.Op switch
            {
                LogicalOp.And => PredicateBuilder.AndAlso(combinedPredicate, nextPredicate),
                LogicalOp.Or => PredicateBuilder.OrElse(combinedPredicate, nextPredicate),
                _ => throw new FilterDispatchException($"Unknown LogicalOp '{group.Op}'.")
            };
        }
        return combinedPredicate;
    }
}
