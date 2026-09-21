namespace Filtering.Net.Generator;

internal sealed record TypedValueTypeReference(
    string ValueClrType,
    string OwnerLabel,
    LocationInfo? Location);

// JsonElement-only types (built-in scalar, unary operators) are skipped — they don't go
// through JsonSerializer.Deserialize<T> and therefore never need AOT registration.
internal static class TypedValueTypeCollector
{
    // One reference per distinct (value type, declaration site): a profile shared by several [Map]s
    // carries the same operator entry once per property, and every [MapNested] splice path copies it
    // again, which would stack identical warnings on one source line.
    public static IEnumerable<TypedValueTypeReference> Collect(FilterClassModel model)
    {
        var reportedSites = new HashSet<(string ValueClrType, LocationInfo? Location)>();

        foreach (var property in model.Properties)
        {
            if (!property.HasTypedValueOperator) continue;
            // A spliced mapping is reported by the nested filter class's own model.
            if (property.SourceFilterClassFqn is not null) continue;

            foreach (var customOperator in property.CustomOperators)
            {
                if (customOperator.ValueClrType is null) continue;
                if (!reportedSites.Add((customOperator.ValueClrType, customOperator.Location))) continue;

                yield return new TypedValueTypeReference(
                    customOperator.ValueClrType,
                    $"[FilterOperator(\"{customOperator.OperatorName}\")] on '{property.PropertyName}'",
                    Location: customOperator.Location);
            }
        }

        foreach (var propertyOverride in model.Overrides)
        {
            if (!propertyOverride.HasTypedValueOperator) continue;

            foreach (var overrideOperator in propertyOverride.Operators)
            {
                if (overrideOperator.ValueClrType is null) continue;
                if (!reportedSites.Add((overrideOperator.ValueClrType, overrideOperator.Location))) continue;

                yield return new TypedValueTypeReference(
                    overrideOperator.ValueClrType,
                    $"[PropertyMap] override '{propertyOverride.PropertyName}' operator '{overrideOperator.Name}'",
                    Location: overrideOperator.Location);
            }
        }
    }
}
