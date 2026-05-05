using System.Collections.Generic;

namespace Filtering.Net.Generator;

/// <summary>
/// Identifies every typed-value type referenced in a <see cref="FilterClassModel"/>:
/// types from <c>[FilterOperator]</c>-declared custom operators on profiles, and types
/// from <c>[PropertyMap]</c> override operator lambdas. Used by the FN1008 analyzer pass.
/// </summary>
internal sealed record TypedValueTypeReference(
    string ValueClrType,
    string OwnerLabel,
    LocationInfo? Location);

/// <summary>
/// Walks a <see cref="FilterClassModel"/> and yields every typed-value-type reference.
/// JsonElement-only types (built-in profile scalar extractions, unary operators) are skipped
/// because they do not go through <c>JsonSerializer.Deserialize&lt;T&gt;</c> at runtime.
/// </summary>
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
