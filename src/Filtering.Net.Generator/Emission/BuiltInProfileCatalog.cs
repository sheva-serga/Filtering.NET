namespace Filtering.Net.Generator;

internal static class BuiltInProfileCatalog
{
    // Unknown (custom-profile) operators default to Scalar; the emitter has full lambda metadata
    // for those and never reaches the default for ambiguous shapes.
    public static OperatorShape ShapeOf(string profileFullName, string operatorName)
    {
        if (operatorName == "isNull") return OperatorShape.None;
        if (operatorName == "in") return OperatorShape.Array;
        return OperatorShape.Scalar;
    }

    public static bool IsBuiltIn(string profileFullName) =>
        profileFullName.StartsWith("Filtering.Net.", StringComparison.Ordinal)
        && !profileFullName.StartsWith("Filtering.Net.Generated.", StringComparison.Ordinal);

    public static readonly HashSet<string> ComparisonOperators = new(StringComparer.Ordinal)
    {
        "eq", "ne", "gt", "gte", "lt", "lte"
    };
}
