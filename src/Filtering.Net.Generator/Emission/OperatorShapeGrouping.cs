namespace Filtering.Net.Generator;

// Buckets allowed operators by value shape so the emitter produces one switch arm per shape.
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

    public IReadOnlyList<string> ScalarOperators { get; }
    public IReadOnlyList<string> ArrayOperators { get; }
    public IReadOnlyList<string> NoneOperators { get; }
    // Each custom operator has its own value type and can't share a TryGet call with others.
    public IReadOnlyList<CustomOperatorModel> CustomOperators { get; }

    public static OperatorShapeGrouping Build(PropertyMappingModel property)
    {
        var scalarOperators = new List<string>();
        var arrayOperators = new List<string>();
        var noneOperators = new List<string>();
        var customOperators = new List<CustomOperatorModel>();

        var isBuiltInProfile = BuiltInProfileCatalog.IsBuiltIn(property.ProfileFullName);

        foreach (var operatorName in property.AllowedOperators)
        {
            // Built-in operator names inherited via BasedOn don't get custom metadata;
            // they fall through to the built-in shape catalog below.
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
