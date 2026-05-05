namespace Filtering.Net.Generator;

/// <summary>
/// Buckets a property's allowed operators by their value shape (Scalar / Array / None / Custom)
/// so the leaf-validator emitter can produce a single grouped <c>switch</c> arm per shape
/// instead of one arm per operator. See Refactor 3 in the v1 cleanup pass.
/// </summary>
internal sealed class OperatorShapeGrouping
{
    private OperatorShapeGrouping(
        IReadOnlyList<string> scalarOperators,
        IReadOnlyList<string> arrayOperators,
        IReadOnlyList<string> noneOperators,
        IReadOnlyList<CustomOperatorModel> customOperators)
    {
        ScalarOperators = scalarOperators;
        ArrayOperators = arrayOperators;
        NoneOperators = noneOperators;
        CustomOperators = customOperators;
    }

    /// <summary>Operators that take a single scalar value (eq, ne, gt, gte, lt, lte,
    /// contains, startsWith, endsWith). Share one grouped switch arm.</summary>
    public IReadOnlyList<string> ScalarOperators { get; }

    /// <summary>Operators that take a JSON array value (currently just <c>in</c>).
    /// Share one grouped switch arm.</summary>
    public IReadOnlyList<string> ArrayOperators { get; }

    /// <summary>Operators that take no value at all (currently just <c>isNull</c>).
    /// Share one grouped switch arm.</summary>
    public IReadOnlyList<string> NoneOperators { get; }

    /// <summary>Custom-profile operators that need their own switch arm. Each one carries
    /// its own value type (from its lambda's <c>TArg</c>) so they can't share a TryGet call
    /// with each other or with the built-in operators.</summary>
    public IReadOnlyList<CustomOperatorModel> CustomOperators { get; }

    /// <summary>Bucketises <paramref name="property"/>'s <c>AllowedOperators</c> by shape.
    /// Built-in operators land in the matching scalar/array/none bucket. Operators declared
    /// on a custom (non-built-in) profile go in the per-operator <see cref="CustomOperators"/>
    /// bucket — keyed by their pre-extracted lambda metadata so the emitter can read the
    /// value type and parameter name.</summary>
    public static OperatorShapeGrouping Build(PropertyMappingModel property)
    {
        var scalarOperators = new List<string>();
        var arrayOperators = new List<string>();
        var noneOperators = new List<string>();
        var customOperators = new List<CustomOperatorModel>();

        var isBuiltInProfile = BuiltInProfileCatalog.IsBuiltIn(property.ProfileFullName);

        foreach (var operatorName in property.AllowedOperators)
        {
            // On a custom profile, prefer the per-operator metadata if we successfully
            // extracted it (lambda body, value type, parameter names). Built-in operator
            // names that the custom profile inherits via [FilterProfile(BasedOn=…)] don't
            // get custom metadata — they fall through to the built-in shape catalog.
            if (!isBuiltInProfile)
            {
                CustomOperatorModel? customMetadata = null;
                foreach (var candidate in property.CustomOperators)
                {
                    if (candidate.OperatorName == operatorName)
                    {
                        customMetadata = candidate;
                        break;
                    }
                }
                if (customMetadata is not null)
                {
                    customOperators.Add(customMetadata);
                    continue;
                }
            }

            switch (BuiltInProfileCatalog.ShapeOf(property.ProfileFullName, operatorName))
            {
                case OperatorShape.None:
                    noneOperators.Add(operatorName);
                    break;
                case OperatorShape.Array:
                    arrayOperators.Add(operatorName);
                    break;
                case OperatorShape.Scalar:
                default:
                    scalarOperators.Add(operatorName);
                    break;
            }
        }

        return new OperatorShapeGrouping(scalarOperators, arrayOperators, noneOperators, customOperators);
    }
}
