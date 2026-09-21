#pragma warning disable IDE0130 // Namespace does not match folder structure

using System.Text.Json;

namespace Filtering.Net;

internal static class FilterTreeValidator
{
    private const string RootPath = "where";

    public static FilterValidationResult Validate<TEntity>(FilterSchema<TEntity> schema, FilterNode? where)
    {
        if (where is null) return FilterValidationResult.Success;

        var validationErrors = new List<FilterValidationError>();
        var serializerOptions = schema.SerializerOptions;
        var leafCount = 0;
        ValidateNode(schema, where, RootPath, depth: 1, ref leafCount, validationErrors, serializerOptions);

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
            if (string.IsNullOrEmpty(sortItem.Field))
            {
                validationErrors.Add(new FilterValidationError(
                    $"sort[{sortIndex}].field",
                    FilterValidationCode.NotSortable,
                    "A sort item must name a field."));
                continue;
            }
            if (!schema.TryGetProperty(sortItem.Field, out var property) || !property.Sortable)
            {
                validationErrors.Add(new FilterValidationError(
                    $"sort[{sortIndex}].field",
                    FilterValidationCode.NotSortable,
                    $"Field '{sortItem.Field}' is not configured as sortable.",
                    Field: sortItem.Field));
                continue;
            }
            if (sortItem.Dir is { } requestedDirection && requestedDirection is not (SortDir.Asc or SortDir.Desc))
            {
                validationErrors.Add(new FilterValidationError(
                    $"sort[{sortIndex}].dir",
                    FilterValidationCode.InvalidSortDirection,
                    $"Sort direction '{(int)requestedDirection}' is not Asc or Desc.",
                    Field: sortItem.Field));
            }
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
        JsonSerializerOptions? serializerOptions)
    {
        if (depth > schema.Settings.MaxNestingDepth)
        {
            // Reported per node that crosses the limit; that node's subtree is not walked.
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
            if (group.Op is not (LogicalOp.And or LogicalOp.Or or LogicalOp.Not))
            {
                errors.Add(new FilterValidationError(
                    path,
                    FilterValidationCode.InvalidNodeShape,
                    $"Group combinator '{(int)group.Op}' is not And, Or, or Not."));
                return;
            }
            if (group.Op == LogicalOp.Not && group.Children.Count != 1)
            {
                errors.Add(new FilterValidationError(
                    path,
                    FilterValidationCode.InvalidNodeShape,
                    $"A 'not' group requires exactly one child, got {group.Children.Count}."));
                return;
            }
            var combinatorSegment = group.Op switch
            {
                LogicalOp.And => "and",
                LogicalOp.Or => "or",
                _ => "not"
            };
            for (var childIndex = 0; childIndex < group.Children.Count; childIndex++)
            {
                var childPath = $"{path}.{combinatorSegment}[{childIndex}]";
                ValidateNode(schema, group.Children[childIndex], childPath, depth + 1, ref leafCount, errors, serializerOptions);
            }
            return;
        }

        if (node is FilterLeaf leaf)
        {
            leafCount++;
            if (schema.TryGetProperty(leaf.Field, out var property))
            {
                property.ValidateLeaf(leaf, path, errors, serializerOptions);
                return;
            }
            errors.Add(new FilterValidationError(
                path,
                FilterValidationCode.UnknownField,
                $"Field '{leaf.Field}' is not configured for filtering.",
                Field: leaf.Field));
            return;
        }

        errors.Add(new FilterValidationError(
            path,
            FilterValidationCode.InvalidNodeShape,
            $"'{node.GetType().Name}' is not a FilterGroup or FilterLeaf."));
    }

    private static FilterValidationResult ToResult(List<FilterValidationError> validationErrors) =>
        validationErrors.Count == 0 ? FilterValidationResult.Success : new FilterValidationResult(validationErrors);
}
