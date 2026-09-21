using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

internal static class FilterPredicateComposer
{
    public static Expression<Func<TEntity, bool>> Compose<TEntity>(FilterSchema<TEntity> schema, FilterNode node) =>
        ComposeNode(schema, node, schema.SerializerOptions);

    private static Expression<Func<TEntity, bool>> ComposeNode<TEntity>(FilterSchema<TEntity> schema, FilterNode node, JsonSerializerOptions? serializerOptions)
    {
        if (node is FilterGroup group) return ComposeGroup(schema, group, serializerOptions);
        if (node is FilterLeaf leaf)
        {
            if (!schema.TryGetProperty(leaf.Field, out var property))
                throw new FilterDispatchException($"Unknown field '{leaf.Field}' (validation should have caught this).");
            return property.BuildPredicate(leaf, serializerOptions);
        }
        throw new FilterDispatchException($"'{node.GetType().Name}' is not a FilterGroup or FilterLeaf (validation should have caught this).");
    }

    private static Expression<Func<TEntity, bool>> ComposeGroup<TEntity>(FilterSchema<TEntity> schema, FilterGroup group, JsonSerializerOptions? serializerOptions)
    {
        if (group.Children.Count == 0)
            throw new FilterDispatchException("Group has no children (validation should have caught this).");

        if (group.Op == LogicalOp.Not)
        {
            if (group.Children.Count != 1)
                throw new FilterDispatchException("Not group requires exactly one child (validation should have caught this).");
            return PredicateBuilder.Not(ComposeNode(schema, group.Children[0], serializerOptions));
        }

        // Checked before the accumulator is seeded, because a single-child group never enters the loop below.
        if (group.Op is not (LogicalOp.And or LogicalOp.Or))
            throw new FilterDispatchException($"Unknown LogicalOp '{(int)group.Op}' (validation should have caught this).");

        var combinedPredicate = ComposeNode(schema, group.Children[0], serializerOptions);
        for (var childIndex = 1; childIndex < group.Children.Count; childIndex++)
        {
            var nextPredicate = ComposeNode(schema, group.Children[childIndex], serializerOptions);
            combinedPredicate = group.Op == LogicalOp.And
                ? PredicateBuilder.AndAlso(combinedPredicate, nextPredicate)
                : PredicateBuilder.OrElse(combinedPredicate, nextPredicate);
        }
        return combinedPredicate;
    }
}
