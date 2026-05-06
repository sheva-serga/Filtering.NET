using System.Collections.Generic;

namespace Filtering.Net.Generator;

internal sealed record TypedValueTypeReference(
    string ValueClrType,
    string OwnerLabel,
    LocationInfo? Location);

// JsonElement-only types (built-in scalar, unary operators) are skipped — they don't go
// through JsonSerializer.Deserialize<T> and therefore never need AOT registration.
internal static class TypedValueTypeCollector
{
    public static IEnumerable<TypedValueTypeReference> Collect(FilterClassModel model)
    {
        foreach (var property in model.Properties)
        {
            if (!property.HasTypedValueOperator) continue;

            foreach (var customOperator in property.CustomOperators)
            {
                if (customOperator.ValueClrType is null) continue;

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

                yield return new TypedValueTypeReference(
                    overrideOperator.ValueClrType,
                    $"[PropertyMap] override '{propertyOverride.PropertyName}' operator '{overrideOperator.Name}'",
                    Location: overrideOperator.Location);
            }
        }
    }
}
