namespace Filtering.Net.Generator;

/// <summary>
/// Helpers for distinguishing built-in profile shapes from user-defined custom profiles
/// during code emission. Built-in profiles (the per-CLR-type filter classes shipped from
/// <c>Filtering.Net</c> plus auto-emitted per-enum profiles) own their own
/// <c>TryGetValue</c>/<c>TryGetArray</c> helpers; user-defined custom profiles need the
/// emitter to walk their <c>BasedOn</c> chain.
/// </summary>
internal static class BuiltInProfileCatalog
{
    /// <summary>Returns the <see cref="OperatorShape"/> for a built-in operator on a profile.
    /// For unknown (custom-profile) operators, defaults to <see cref="OperatorShape.Scalar"/>
    /// because the emitter has full lambda metadata available for custom profiles.</summary>
    public static OperatorShape ShapeOf(string profileFullName, string operatorName)
    {
        if (operatorName == "isNull") return OperatorShape.None;
        if (operatorName == "in") return OperatorShape.Array;
        return OperatorShape.Scalar;
    }

    /// <summary>True when the profile name corresponds to one of the built-in profiles
    /// shipped from <c>Filtering.Net</c> (recognised by namespace prefix; auto-emitted enum
    /// profiles under <c>Filtering.Net.Generated</c> are excluded — they have their own
    /// recognition path in <see cref="ProfileResolver.IsAutoEmittedEnumProfile"/>).</summary>
    public static bool IsBuiltIn(string profileFullName) =>
        profileFullName.StartsWith("Filtering.Net.", StringComparison.Ordinal)
        && !profileFullName.StartsWith("Filtering.Net.Generated.", StringComparison.Ordinal);

    /// <summary>The full set of comparison operators supported across all built-in profiles —
    /// used by <see cref="OperatorEmissionExpressions"/> to know when to emit a binary
    /// <c>&gt;</c>/<c>&gt;=</c>/etc. expression vs a method call.</summary>
    public static readonly HashSet<string> ComparisonOperators = new(StringComparer.Ordinal)
    {
        "eq", "ne", "gt", "gte", "lt", "lte"
    };
}
