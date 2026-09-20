namespace Filtering.Net;

internal static class FilterTreeValidator
{
    private const string RootPath = "where";

    public static FilterValidationResult Validate<TEntity>(FilterSchema<TEntity> schema, FilterNode? where)
    {
        if (where is null) return FilterValidationResult.Success;

        var validationErrors = new List<FilterValidationError>();
        var valueContext = new FilterValueContext(schema.SerializerOptions);
        var leafCount = 0;
        ValidateNode(schema, where, RootPath, depth: 1, ref leafCount, validationErrors, valueContext);

        if (leafCount > schema.Settings.MaxLeafConditions)
        {
            validationErrors.Add(new FilterValidationError(
                RootPath,
                FilterValidationCode.TooManyConditions,
                $"Filter has {leafCount} conditions, exceeding the configured maximum of {schema.Settings.MaxLeafConditions}."));
        }
        return ToResult(validationErrors);
    }

    public static FilterValidationResult ValidateSort<TEntity>(FilterSchema<TEntity> schema, IReadOnlyList<SortItem>? sortItems)
    {
        if (sortItems is null || sortItems.Count == 0) return FilterValidationResult.Success;

        var validationErrors = new List<FilterValidationError>();
        for (var sortIndex = 0; sortIndex < sortItems.Count; sortIndex++)
        {
            var sortItem = sortItems[sortIndex];
            if (schema.TryGetProperty(sortItem.Field, out var property) && property.Sortable) continue;
            validationErrors.Add(new FilterValidationError(
                $"sort[{sortIndex}].field",
                FilterValidationCode.NotSortable,
                $"Field '{sortItem.Field}' is not configured as sortable.",
                Field: sortItem.Field));
        }
        return ToResult(validationErrors);
    }

    private static void ValidateNode<TEntity>(
        FilterSchema<TEntity> schema,
        FilterNode node,
        string path,
        int depth,
        ref int leafCount,
        List<FilterValidationError> errors,
        FilterValueContext valueContext)
    {
        if (depth > schema.Settings.MaxNestingDepth)
        {
            // Reported once at the first node past the limit; its subtree is not walked.
            errors.Add(new FilterValidationError(
                path,
                FilterValidationCode.NestingTooDeep,
                $"Filter nesting depth exceeds the configured maximum of {schema.Settings.MaxNestingDepth}."));
            return;
        }

        if (node is FilterGroup group)
        {
            if (group.Children.Count == 0)
            {
                errors.Add(new FilterValidationError(path, FilterValidationCode.GroupEmpty, "Group has no children."));
                return;
            }
            var combinatorSegment = group.Op switch
            {
                LogicalOp.And => "and",
                LogicalOp.Or => "or",
                LogicalOp.Not => "not",
                _ => "unknown"
            };
            for (var childIndex = 0; childIndex < group.Children.Count; childIndex++)
            {
                var childPath = $"{path}.{combinatorSegment}[{childIndex}]";
                ValidateNode(schema, group.Children[childIndex], childPath, depth + 1, ref leafCount, errors, valueContext);
            }
            return;
        }

        if (node is FilterLeaf leaf)
        {
            leafCount++;
            if (schema.TryGetProperty(leaf.Field, out var property))
            {
                property.ValidateLeaf(leaf, path, errors, valueContext);
                return;
            }
            errors.Add(new FilterValidationError(
                path,
                FilterValidationCode.UnknownField,
                $"Field '{leaf.Field}' is not configured for filtering.",
                Field: leaf.Field));
            return;
        }

        errors.Add(new FilterValidationError(path, FilterValidationCode.InvalidValueType, "Unknown FilterNode subtype."));
    }

    private static FilterValidationResult ToResult(List<FilterValidationError> validationErrors) =>
        validationErrors.Count == 0 ? FilterValidationResult.Success : new FilterValidationResult(validationErrors);
}
